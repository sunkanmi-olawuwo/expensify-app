using System.Reflection;
using Expensify.Api.Extensions;
using Expensify.Api.Middleware;
using Expensify.Api.OpenTelemetry;
using Expensify.Api.SignalR;
using Expensify.Common.Application;
using Expensify.Common.Application.SignalR;
using Expensify.Common.Infrastructure;
using Expensify.Common.Infrastructure.Configuration;
using Expensify.Modules.Dashboard.Infrastructure;
using Expensify.Modules.Expenses.Infrastructure;
using Expensify.Modules.Income.Infrastructure;
using Expensify.Modules.Users.Infrastructure;
using Expensify.Modules.Users.Infrastructure.Database;
using Serilog;
using static Microsoft.Extensions.Hosting.Extensions;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Host.UseSerilog((context, loggerConfig) => loggerConfig.ReadFrom.Configuration(context.Configuration));

builder.ConfigureService(builder.Environment);

//add common config here
Assembly[] moduleApplicationAssemblies = [
    Expensify.Modules.Users.Application.AssemblyReference.Assembly,
    Expensify.Modules.Expenses.Application.AssemblyReference.Assembly,
    Expensify.Modules.Income.Application.AssemblyReference.Assembly,
    Expensify.Modules.Dashboard.Application.AssemblyReference.Assembly,
    ];

builder.Services.AddApplication(moduleApplicationAssemblies);

string databaseConnectionString = builder.Configuration.GetConnectionStringOrThrow("Database");
string redisConnectionString = builder.Configuration.GetConnectionStringOrThrow("Cache");

builder.Services.AddInfrastructure(
    builder.Configuration,
    [],
    databaseConnectionString,
    redisConnectionString);
builder.Services.AddApiRateLimiting(builder.Configuration);

builder.AddServiceDefaults(new ServiceDefaultSettings(databaseConnectionString, redisConnectionString, DiagnosticsConfig.ServiceName));

builder.Configuration.AddModuleConfiguration(["users", "expenses", "income", "dashboard"]);

builder.Services.AddUsersModule(builder.Configuration);
builder.Services.AddExpensesModule(builder.Configuration);
builder.Services.AddIncomeModule(builder.Configuration);
builder.Services.AddDashboardModule(builder.Configuration);
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

app.UseForwardedHeaders();

app.UseAuthentication();

app.UseRateLimiter();

app.UseAuthorization();
app.UseCheckRevocatedTokens();
app.MapDefaultEndpoints();
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

await app.RunAsync();
