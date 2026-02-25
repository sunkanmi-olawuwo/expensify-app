using System.Reflection;
using Expensify.Modules.Income.Domain.Incomes;
using Expensify.Modules.Income.Infrastructure;
using IncomeEntity = Expensify.Modules.Income.Domain.Incomes.Income;

namespace Expensify.Modules.Income.ArchitectureTests.Abstractions;

public abstract class BaseTest
{
    protected static readonly Assembly ApplicationAssembly = typeof(Income.Application.AssemblyReference).Assembly;

    protected static readonly Assembly DomainAssembly = typeof(IncomeEntity).Assembly;

    protected static readonly Assembly InfrastructureAssembly = typeof(IncomeModule).Assembly;

    protected static readonly Assembly PresentationAssembly = typeof(Income.Presentation.AssemblyReference).Assembly;
}
