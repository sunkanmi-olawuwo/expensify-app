using System.Reflection;
using NetArchTest.Rules;
using Expensify.ArchitectureTests.Abstractions;
using Expensify.Modules.Users.Domain.Users;
using Expensify.Modules.Users.Infrastructure;

namespace Expensify.ArchitectureTests.Layer;

public class ModuleTests : BaseTest
{
    [Test]
    public void UsersModule_ShouldNotHaveDependencyOn_AnyOtherModule()
    {
        string[] otherModules = [ApiNamespace];
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

    
}
