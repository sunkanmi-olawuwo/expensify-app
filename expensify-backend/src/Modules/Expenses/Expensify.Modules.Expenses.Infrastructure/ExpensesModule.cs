using Mapster;
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
using Expensify.Modules.Expenses.Application;
using Expensify.Modules.Expenses.Application.Abstractions;
using Expensify.Modules.Expenses.Application.Abstractions.Users;
using Expensify.Modules.Expenses.Domain.Categories;
using Expensify.Modules.Expenses.Domain.Expenses;
using Expensify.Modules.Expenses.Domain.Tags;
using Expensify.Modules.Expenses.Infrastructure.Categories;
using Expensify.Modules.Expenses.Infrastructure.Database;
using Expensify.Modules.Expenses.Infrastructure.Expenses;
using Expensify.Modules.Expenses.Infrastructure.Inbox;
using Expensify.Modules.Expenses.Infrastructure.Outbox;
using Expensify.Modules.Expenses.Infrastructure.Policies;
using Expensify.Modules.Expenses.Infrastructure.Tags;
using Expensify.Modules.Expenses.Infrastructure.Users;

namespace Expensify.Modules.Expenses.Infrastructure;

public static class ExpensesModule
{
    public static IServiceCollection AddExpensesModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDomainEventHandlers();
        services.AddIntegrationEventHandlers();
        services.AddInfrastructure(configuration);

        Type[] configurationAssemblyMarkerTypes =
        [
            typeof(AssemblyReference),
            typeof(ExpensesModule),
            typeof(Presentation.AssemblyReference)
        ];

        services.AddMapster(configurationAssemblyMarkerTypes);
        services.AddCarterModules(typeof(Presentation.AssemblyReference));

        return services;
    }

    private static void AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<ExpensesDbContext>((sp, options) =>
            options
                .UseNpgsql(
                    configuration.GetConnectionString("Database"),
                    npgsqlOptions => npgsqlOptions
                        .MigrationsHistoryTable(HistoryRepository.DefaultTableName, Schemas.Expenses))
                .AddInterceptors(sp.GetRequiredService<AuditableInterceptor>())
                .AddInterceptors(sp.GetRequiredService<InsertOutboxMessagesInterceptor>())
                .UseSnakeCaseNamingConvention());

        services.AddScoped<IExpenseRepository, ExpenseRepository>();
        services.AddScoped<IExpenseCategoryRepository, ExpenseCategoryRepository>();
        services.AddScoped<IExpenseTagRepository, ExpenseTagRepository>();
        services.AddScoped<IUserSettingsService, UserSettingsService>();

        services.AddSingleton<IPolicyFactory, ExpensesPolicyFactory>();
        services.AddScoped<IExpensesUnitOfWork>(sp => sp.GetRequiredService<ExpensesDbContext>());

        services.Configure<OutboxOptions>(configuration.GetSection("Expenses:Outbox"));
        services.ConfigureOptions<ConfigureProcessOutboxJob>();
        services.Configure<InboxOptions>(configuration.GetSection("Expenses:Inbox"));
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

            Type closedIdempotentHandler = typeof(IdempotentIntegrationEventHandler<>).MakeGenericType(integrationEvent);

            services.Decorate(integrationEventHandler, closedIdempotentHandler);
        }
    }
}
