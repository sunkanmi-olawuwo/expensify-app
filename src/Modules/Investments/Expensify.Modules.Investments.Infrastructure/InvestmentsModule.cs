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
using Expensify.Modules.Investments.Application;
using Expensify.Modules.Investments.Application.Abstractions;
using Expensify.Modules.Investments.Application.Abstractions.Users;
using Expensify.Modules.Investments.Domain.Accounts;
using Expensify.Modules.Investments.Domain.Categories;
using Expensify.Modules.Investments.Domain.Contributions;
using Expensify.Modules.Investments.Infrastructure.Accounts;
using Expensify.Modules.Investments.Infrastructure.Categories;
using Expensify.Modules.Investments.Infrastructure.Contributions;
using Expensify.Modules.Investments.Infrastructure.Database;
using Expensify.Modules.Investments.Infrastructure.Inbox;
using Expensify.Modules.Investments.Infrastructure.Outbox;
using Expensify.Modules.Investments.Infrastructure.Policies;
using Expensify.Modules.Investments.Infrastructure.SoftDelete;
using Expensify.Modules.Investments.Infrastructure.Users;

namespace Expensify.Modules.Investments.Infrastructure;

public static class InvestmentsModule
{
    public static IServiceCollection AddInvestmentsModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDomainEventHandlers();
        services.AddIntegrationEventHandlers();
        services.AddInfrastructure(configuration);

        Type[] configurationAssemblyMarkerTypes =
        [
            typeof(AssemblyReference),
            typeof(InvestmentsModule),
            typeof(Presentation.AssemblyReference)
        ];

        services.AddMapster(configurationAssemblyMarkerTypes);
        services.AddCarterModules(typeof(Presentation.AssemblyReference));

        return services;
    }

    private static void AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<InvestmentsDbContext>((sp, options) =>
            options
                .UseNpgsql(
                    configuration.GetConnectionString("Database"),
                    npgsqlOptions => npgsqlOptions.MigrationsHistoryTable(HistoryRepository.DefaultTableName, Schemas.Investments))
                .AddInterceptors(sp.GetRequiredService<AuditableInterceptor>())
                .AddInterceptors(sp.GetRequiredService<InsertOutboxMessagesInterceptor>())
                .UseSnakeCaseNamingConvention());

        services.AddScoped<IInvestmentAccountRepository, InvestmentAccountRepository>();
        services.AddScoped<IInvestmentCategoryRepository, InvestmentCategoryRepository>();
        services.AddScoped<IInvestmentContributionRepository, InvestmentContributionRepository>();
        services.AddScoped<IUserSettingsService, UserSettingsService>();

        services.AddSingleton<IPolicyFactory, InvestmentsPolicyFactory>();
        services.AddScoped<IInvestmentsUnitOfWork>(sp => sp.GetRequiredService<InvestmentsDbContext>());

        services.Configure<OutboxOptions>(configuration.GetSection("Investments:Outbox"));
        services.ConfigureOptions<ConfigureProcessOutboxJob>();
        services.Configure<InboxOptions>(configuration.GetSection("Investments:Inbox"));
        services.ConfigureOptions<ConfigureProcessInboxJob>();
        services.Configure<SoftDeleteOptions>(configuration.GetSection("Investments:SoftDelete"));
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
