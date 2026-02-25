using System.Reflection;
using Expensify.Modules.Expenses.Domain.Expenses;
using Expensify.Modules.Expenses.Infrastructure;

namespace Expensify.Modules.Expenses.ArchitectureTests.Abstractions;

public abstract class BaseTest
{
    protected static readonly Assembly ApplicationAssembly = typeof(Expenses.Application.AssemblyReference).Assembly;

    protected static readonly Assembly DomainAssembly = typeof(Expense).Assembly;

    protected static readonly Assembly InfrastructureAssembly = typeof(ExpensesModule).Assembly;

    protected static readonly Assembly PresentationAssembly = typeof(Expenses.Presentation.AssemblyReference).Assembly;
}
