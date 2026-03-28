using NetArchTest.Rules;
using Expensify.Modules.Dashboard.ArchitectureTests.Abstractions;

namespace Expensify.Modules.Dashboard.ArchitectureTests.Layers;

public class LayerTests : BaseTest
{
    [Test]
    public void DomainLayer_ShouldNotHaveDependencyOn_ApplicationLayer()
    {
        Types.InAssembly(DomainAssembly)
            .Should()
            .NotHaveDependencyOn(ApplicationAssembly.GetName().Name)
            .GetResult()
            .ShouldBeSuccessful();
    }

    [Test]
    public void DomainLayer_ShouldNotHaveDependencyOn_InfrastructureLayer()
    {
        Types
            .InAssembly(DomainAssembly)
            .Should()
            .NotHaveDependencyOn(InfrastructureAssembly.GetName().Name)
            .GetResult()
            .ShouldBeSuccessful();
    }

    [Test]
    public void ApplicationLayer_ShouldNotHaveDependencyOn_InfrastructureLayer()
    {
        Types.InAssembly(ApplicationAssembly)
            .Should()
            .NotHaveDependencyOn(InfrastructureAssembly.GetName().Name)
            .GetResult()
            .ShouldBeSuccessful();
    }

    [Test]
    public void ApplicationLayer_ShouldNotHaveDependencyOn_PresentationLayer()
    {
        Types.InAssembly(ApplicationAssembly)
            .Should()
            .NotHaveDependencyOn(PresentationAssembly.GetName().Name)
            .GetResult()
            .ShouldBeSuccessful();
    }

    [Test]
    public void PresentationLayer_ShouldNotHaveDependencyOn_InfrastructureLayer()
    {
        Types.InAssembly(PresentationAssembly)
            .Should()
            .NotHaveDependencyOn(InfrastructureAssembly.GetName().Name)
            .GetResult()
            .ShouldBeSuccessful();
    }
}
