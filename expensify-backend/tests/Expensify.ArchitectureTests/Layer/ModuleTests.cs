using System.Reflection;
using NetArchTest.Rules;
using Expensify.ArchitectureTests.Abstractions;
using Expensify.Modules.Expenses.Domain.Expenses;
using Expensify.Modules.Expenses.Infrastructure;
using Expensify.Modules.Users.Domain.Users;
using Expensify.Modules.Users.Infrastructure;

namespace Expensify.ArchitectureTests.Layer;

public class ModuleTests : BaseTest
{
    [Test]
    public void UsersModule_ShouldNotHaveDependencyOn_AnyOtherModule()
    {
        string[] otherModules = [ApiNamespace, ExpensesNamespace];
        string[] integrationEventsModules = [];

        List<Assembly> usersAssemblies =
        [
            typeof(User).Assembly,
            Modules.Users.Application.AssemblyReference.Assembly,
            Modules.Users.Presentation.AssemblyReference.Assembly,
            typeof(UsersModule).Assembly
        ];

        Types.InAssemblies(usersAssemblies)
            .That()
            .DoNotHaveDependencyOnAny(integrationEventsModules)
            .Should()
            .NotHaveDependencyOnAny(otherModules)
            .GetResult()
            .ShouldBeSuccessful();
    }

    [Test]
    public void ExpensesModule_ShouldNotHaveDependencyOn_AnyOtherModule()
    {
        string[] otherModules = [ApiNamespace, UsersNamespace, UsersIntegrationEventsNamespace];
        string[] integrationEventsModules = [];

        List<Assembly> expensesAssemblies =
        [
            typeof(Expense).Assembly,
            Modules.Expenses.Application.AssemblyReference.Assembly,
            Modules.Expenses.Presentation.AssemblyReference.Assembly,
            typeof(ExpensesModule).Assembly
        ];

        Types.InAssemblies(expensesAssemblies)
            .That()
            .DoNotHaveDependencyOnAny(integrationEventsModules)
            .Should()
            .NotHaveDependencyOnAny(otherModules)
            .GetResult()
            .ShouldBeSuccessful();
    }
}
