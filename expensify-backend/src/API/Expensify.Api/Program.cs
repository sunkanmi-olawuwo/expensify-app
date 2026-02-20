using System.Reflection;
using Expensify.Api.Extensions;
using Expensify.Api.Middleware;
using Expensify.Api.OpenTelemetry;
using Expensify.Api.SignalR;
using Expensify.Common.Application;
using Expensify.Common.Application.SignalR;
using Expensify.Common.Infrastructure;
using Expensify.Common.Infrastructure.Configuration;
using Expensify.Modules.Users.Infrastructure;
using Expensify.Modules.Users.Infrastructure.Database;
using Serilog;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Host.UseSerilog((context, loggerConfig) => loggerConfig.ReadFrom.Configuration(context.Configuration));

builder.ConfigureService(builder.Environment);

//add common config here
Assembly[] moduleApplicationAssemblies = [
    Expensify.Modules.Users.Application.AssemblyReference.Assembly,
    ];

builder.Services.AddApplication(moduleApplicationAssemblies);

string databaseConnectionString = builder.Configuration.GetConnectionStringOrThrow("Database");
string redisConnectionString = builder.Configuration.GetConnectionStringOrThrow("Cache");

builder.Services.AddInfrastructure(
    builder.Configuration,
    DiagnosticsConfig.ServiceName,
    [],
    databaseConnectionString,
    redisConnectionString);

builder.Services.AddHealthChecks()
     .AddNpgSql(databaseConnectionString)
.AddRedis(redisConnectionString);

builder.Configuration.AddModuleConfiguration(["users"]);

builder.Services.AddUsersModule(builder.Configuration);
builder.Services.AddSingleton<ISignalrSubscriptionCache, InMemorySignalrSubscriptionCache>();

WebApplication app = builder.Build();

// Configure the HTTP request pipeline.

bool isNswagExecution = Environment.GetCommandLineArgs().Any(arg => arg.Contains("nswag", StringComparison.OrdinalIgnoreCase));
if (app.Environment.IsDevelopment() && !isNswagExecution)
{
    app.ApplyMigrations();

    using IServiceScope scope = app.Services.CreateScope();
    UserSeedService userSeedService = scope.ServiceProvider.GetRequiredService<UserSeedService>();
    await userSeedService.SeedUsersAsync();
}

app.Configure();

app.UseLogContextTraceLogging();

app.UseSerilogRequestLogging();

app.UseAuthentication();

app.UseAuthorization();
app.UseCheckRevocatedTokens();

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

await app.RunAsync();
