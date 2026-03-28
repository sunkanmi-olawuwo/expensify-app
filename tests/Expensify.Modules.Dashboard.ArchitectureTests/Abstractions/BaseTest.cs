using System.Reflection;
using Expensify.Modules.Dashboard.Domain.Policies;
using Expensify.Modules.Dashboard.Infrastructure;

namespace Expensify.Modules.Dashboard.ArchitectureTests.Abstractions;

public abstract class BaseTest
{
    protected static readonly Assembly ApplicationAssembly = typeof(Dashboard.Application.AssemblyReference).Assembly;

    protected static readonly Assembly DomainAssembly = typeof(DashboardPolicyConsts).Assembly;

    protected static readonly Assembly InfrastructureAssembly = typeof(DashboardModule).Assembly;

    protected static readonly Assembly PresentationAssembly = typeof(Dashboard.Presentation.AssemblyReference).Assembly;
}
