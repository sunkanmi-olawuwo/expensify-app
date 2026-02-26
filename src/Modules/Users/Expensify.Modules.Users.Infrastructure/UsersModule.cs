using Mapster;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Expensify.Common.Application.Data;
using Expensify.Common.Application.EventBus;
using Expensify.Common.Application.Messaging;
using Expensify.Common.Infrastructure;
using Expensify.Common.Infrastructure.Authorization.Policies;
using Expensify.Common.Infrastructure.Data;
using Expensify.Common.Infrastructure.Outbox;
using Expensify.Modules.Users.Application;
using Expensify.Modules.Users.Application.Abstractions.Identity;
using Expensify.Modules.Users.Domain.Identity;
using Expensify.Modules.Users.Domain.Tokens;
using Expensify.Modules.Users.Domain.Users;
using Expensify.Modules.Users.Infrastructure.Database;
using Expensify.Modules.Users.Infrastructure.Identity;
using Expensify.Modules.Users.Infrastructure.Inbox;
using Expensify.Modules.Users.Infrastructure.Outbox;
using Expensify.Modules.Users.Infrastructure.Token;
using Expensify.Modules.Users.Infrastructure.Users;
using Expensify.Modules.Users.Infrastructure.Users.Policies;

namespace Expensify.Modules.Users.Infrastructure;

public static class UsersModule
{
    public static IServiceCollection AddUsersModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDomainEventHandlers();

        services.AddIntegrationEventHandlers();

        services.AddInfrastructure(configuration);

        Type[] configurationAssemblyMarkerTypes = [
            typeof(AssemblyReference),
            typeof(UsersModule),
            typeof(Presentation.AssemblyReference)];

        services.AddMapster(configurationAssemblyMarkerTypes);
        services.AddCarterModules(typeof(Presentation.AssemblyReference));
        services.AddScoped<UserSeedService>();

        return services;
    }

    private static void AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        
        services.AddTransient<IIdentityProviderService, IdentityProviderService>();

        services.AddDbContext<UsersDbContext>((sp, options) =>
            options
                .UseNpgsql(
                    configuration.GetConnectionString("Database"),
                    npgsqlOptions => npgsqlOptions
                        .MigrationsHistoryTable(HistoryRepository.DefaultTableName, Schemas.Users))
                .AddInterceptors(sp.GetRequiredService<AuditableInterceptor>())
                .AddInterceptors(sp.GetRequiredService<InsertOutboxMessagesInterceptor>())
                .UseSnakeCaseNamingConvention());

        services
            .AddIdentityCore<IdentityUser>(options =>
            {
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = true;
                options.Password.RequiredLength = 8;
            })
            .AddRoles<Role>()
            .AddEntityFrameworkStores<UsersDbContext>()
            .AddSignInManager()
            .AddDefaultTokenProviders();

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddSingleton<IPolicyFactory, UsersPolicyFactory>();

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<UsersDbContext>());

        services.Configure<OutboxOptions>(configuration.GetSection("Users:Outbox"));

        services.ConfigureOptions<ConfigureProcessOutboxJob>();

        services.Configure<InboxOptions>(configuration.GetSection("Users:Inbox"));

        services.ConfigureOptions<ConfigureProcessInboxJob>();
    }

    private static void AddDomainEventHandlers(this IServiceCollection services)
    {
        Type[] domainEventHandlers = [.. AssemblyReference.Assembly
            .GetTypes()
            .Where(t => t.IsAssignableTo(typeof(IDomainEventHandler)))];

        foreach (Type domainEventHandler in domainEventHandlers)
        {
            services.TryAddScoped(domainEventHandler);

            Type domainEvent = domainEventHandler
                .GetInterfaces()
                .Single(i => i.IsGenericType)
                .GetGenericArguments()
                .Single();

            Type closedIdempotentHandler = typeof(IdempotentDomainEventHandler<>).MakeGenericType(domainEvent);

            services.Decorate(domainEventHandler, closedIdempotentHandler);
        }
    }

    private static void AddIntegrationEventHandlers(this IServiceCollection services)
    {
        Type[] integrationEventHandlers = [.. AssemblyReference.Assembly
            .GetTypes()
            .Where(t => t.IsAssignableTo(typeof(IIntegrationEventHandler)))];

        foreach (Type integrationEventHandler in integrationEventHandlers)
        {
            services.TryAddScoped(integrationEventHandler);

            Type integrationEvent = integrationEventHandler
                .GetInterfaces()
                .Single(i => i.IsGenericType)
                .GetGenericArguments()
                .Single();

            Type closedIdempotentHandler =
                typeof(IdempotentIntegrationEventHandler<>).MakeGenericType(integrationEvent);

            services.Decorate(integrationEventHandler, closedIdempotentHandler);
        }
    }
}
