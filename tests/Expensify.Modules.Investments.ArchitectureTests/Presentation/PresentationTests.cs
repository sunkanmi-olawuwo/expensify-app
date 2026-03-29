using NetArchTest.Rules;
using Expensify.Common.Application.EventBus;
using Expensify.Modules.Investments.ArchitectureTests.Abstractions;

namespace Expensify.Modules.Investments.ArchitectureTests.Presentation;

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
