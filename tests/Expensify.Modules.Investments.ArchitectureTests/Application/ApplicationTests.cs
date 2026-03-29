using FluentValidation;
using NetArchTest.Rules;
using Expensify.Common.Application.Messaging;
using Expensify.Modules.Investments.ArchitectureTests.Abstractions;

namespace Expensify.Modules.Investments.ArchitectureTests.Application;

public class ApplicationTests : BaseTest
{
    [Test]
    public void Command_Should_BeSealed()
    {
        Types.InAssembly(ApplicationAssembly)
            .That()
            .ImplementInterface(typeof(ICommand))
            .Or()
            .ImplementInterface(typeof(ICommand<>))
            .Should()
            .BeSealed()
            .GetResult()
            .ShouldBeSuccessful();
    }

    [Test]
    public void Query_Should_BeSealed()
    {
        Types.InAssembly(ApplicationAssembly)
            .That()
            .ImplementInterface(typeof(IQuery<>))
            .Should()
            .BeSealed()
            .GetResult()
            .ShouldBeSuccessful();
    }

    [Test]
    public void CommandHandlers_Should_NotBePublic()
    {
        Types.InAssembly(ApplicationAssembly)
            .That()
            .ImplementInterface(typeof(ICommandHandler<>))
            .Or()
            .ImplementInterface(typeof(ICommandHandler<,>))
            .Should()
            .NotBePublic()
            .GetResult()
            .ShouldBeSuccessful();
    }

    [Test]
    public void QueryHandlers_Should_NotBePublic()
    {
        Types.InAssembly(ApplicationAssembly)
            .That()
            .ImplementInterface(typeof(IQueryHandler<,>))
            .Should()
            .NotBePublic()
            .GetResult()
            .ShouldBeSuccessful();
    }

    [Test]
    public void Validators_Should_BeSealed()
    {
        Types.InAssembly(ApplicationAssembly)
            .That()
            .Inherit(typeof(AbstractValidator<>))
            .Should()
            .BeSealed()
            .GetResult()
            .ShouldBeSuccessful();
    }
}
