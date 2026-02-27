using Mapster;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Expensify.Common.Application.EventBus;
using Expensify.Common.Application.Messaging;
using Expensify.Common.Infrastructure;
using Expensify.Common.Infrastructure.Authorization.Policies;
using Expensify.Common.Infrastructure.Data;
using Expensify.Common.Infrastructure.Outbox;
using Expensify.Modules.Income.Application;
using Expensify.Modules.Income.Application.Abstractions;
using Expensify.Modules.Income.Application.Abstractions.SoftDelete;
using Expensify.Modules.Income.Application.Abstractions.Users;
using Expensify.Modules.Income.Domain.Incomes;
using Expensify.Modules.Income.Infrastructure.Database;
using Expensify.Modules.Income.Infrastructure.Inbox;
using Expensify.Modules.Income.Infrastructure.Incomes;
using Expensify.Modules.Income.Infrastructure.Outbox;
using Expensify.Modules.Income.Infrastructure.Policies;
using Expensify.Modules.Income.Infrastructure.SoftDelete;
using Expensify.Modules.Income.Infrastructure.Users;

namespace Expensify.Modules.Income.Infrastructure;

public static class IncomeModule
{
    public static IServiceCollection AddIncomeModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDomainEventHandlers();
        services.AddIntegrationEventHandlers();
        services.AddInfrastructure(configuration);

        Type[] configurationAssemblyMarkerTypes =
        [
            typeof(AssemblyReference),
            typeof(IncomeModule),
            typeof(Presentation.AssemblyReference)
        ];

        services.AddMapster(configurationAssemblyMarkerTypes);
        services.AddCarterModules(typeof(Presentation.AssemblyReference));

        return services;
    }

    private static void AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<IncomeDbContext>((sp, options) =>
            options
                .UseNpgsql(
                    configuration.GetConnectionString("Database"),
                    npgsqlOptions => npgsqlOptions
                        .MigrationsHistoryTable(HistoryRepository.DefaultTableName, Schemas.Income))
                .AddInterceptors(sp.GetRequiredService<AuditableInterceptor>())
                .AddInterceptors(sp.GetRequiredService<InsertOutboxMessagesInterceptor>())
                .UseSnakeCaseNamingConvention());

        services.AddScoped<IIncomeRepository, IncomeRepository>();
        services.AddScoped<IUserSettingsService, UserSettingsService>();
        services.AddScoped<ISoftDeleteRetentionProvider, SoftDeleteRetentionProvider>();

        services.AddSingleton<IPolicyFactory, IncomePolicyFactory>();
        services.AddScoped<IIncomeUnitOfWork>(sp => sp.GetRequiredService<IncomeDbContext>());

        services.Configure<OutboxOptions>(configuration.GetSection("Income:Outbox"));
        services.ConfigureOptions<ConfigureProcessOutboxJob>();
        services.Configure<InboxOptions>(configuration.GetSection("Income:Inbox"));
        services.ConfigureOptions<ConfigureProcessInboxJob>();
        services.Configure<SoftDeleteOptions>(configuration.GetSection("Income:SoftDelete"));
        services.ConfigureOptions<ConfigureProcessSoftDeletePurgeJob>();
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

            Type closedIdempotentHandler = typeof(IdempotentIntegrationEventHandler<>).MakeGenericType(integrationEvent);

            services.Decorate(integrationEventHandler, closedIdempotentHandler);
        }
    }
}
