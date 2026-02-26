using NetArchTest.Rules;
using Expensify.Common.Application.EventBus;
using Expensify.Modules.Income.ArchitectureTests.Abstractions;

namespace Expensify.Modules.Income.ArchitectureTests.Presentation;

public class PresentationTests : BaseTest
{
    [Test]
    public void IntegrationEventHandlers_Should_NotBePublic()
    {
        Types.InAssembly(PresentationAssembly)
            .That()
            .ImplementInterface(typeof(IIntegrationEventHandler<>))
            .Or()
            .Inherit(typeof(IntegrationEventHandler<>))
            .Should()
            .NotBePublic()
            .GetResult()
            .ShouldBeSuccessful();
    }
}
