using System.Reflection;
using Expensify.Modules.Investments.Domain.Accounts;
using Expensify.Modules.Investments.Infrastructure;

namespace Expensify.Modules.Investments.ArchitectureTests.Abstractions;

public abstract class BaseTest
{
    protected static readonly Assembly ApplicationAssembly = typeof(Investments.Application.AssemblyReference).Assembly;

    protected static readonly Assembly DomainAssembly = typeof(InvestmentAccount).Assembly;

    protected static readonly Assembly InfrastructureAssembly = typeof(InvestmentsModule).Assembly;

    protected static readonly Assembly PresentationAssembly = typeof(Investments.Presentation.AssemblyReference).Assembly;
}
