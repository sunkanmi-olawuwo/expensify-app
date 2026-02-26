using System.Reflection;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Expensify.Common.Application.SignalR;

namespace Expensify.Common.Infrastructure.SignalR;

public static class SignalRExtensions
{
    public static IServiceCollection AddUiEvents(this IServiceCollection services,
       params Type[] markerTypes) =>
       services.AddOpenGenericNotificationHandlers(
               typeof(IUiEvent),
               typeof(SignalrNotificationHandler<>),
               markerTypes)
           .Scan(scan => scan
               .FromAssembliesOf(markerTypes)
               .AddClasses(classes => classes.AssignableTo(typeof(IUiEventMapper<>)))
               .AsImplementedInterfaces()
               .WithTransientLifetime());

    public static IServiceCollection AddOpenGenericNotificationHandlers(
       this IServiceCollection services,
       Type closingGenericType,
       Type openGenericImplementationType,
       params Type[] handlerAssemblyMarkerTypes)
    {
        // Register open generic types manually to avoid duplicate handler instances.
        // https://github.com/jbogard/MediatR/issues/702
        // https://github.com/dotnet/runtime/issues/65145

        static bool IsTypeOfClosingType(TypeInfo t, Type closingType) =>
            closingType.IsInterface
                ? closingType.IsAssignableFrom(t)
                : t.IsSubclassOf(closingType);

        foreach (Type assemblyMarkerType in handlerAssemblyMarkerTypes)
        {
            IEnumerable<TypeInfo> types = assemblyMarkerType.Assembly.DefinedTypes
                .Where(t => !t.IsAbstract && IsTypeOfClosingType(t, closingGenericType));
            foreach (TypeInfo? type in types)
            {
                Type serviceType = typeof(INotificationHandler<>).MakeGenericType(type);
                Type implementationType = openGenericImplementationType.MakeGenericType(type);
                services.AddTransient(serviceType, implementationType);
            }
        }

        return services;
    }
}


