using Expensify.Common.Domain;
using Expensify.Modules.Users.Application.Abstractions.Identity;
using Expensify.Modules.Users.Application.Users.Command.ForgotPassword;
using NSubstitute;

namespace Expensify.Modules.Users.UnitTests.Application.Users.Command;

[TestFixture]
internal sealed class ForgotPasswordCommandHandlerTests
{
    private IIdentityProviderService _identityProviderService = null!;
    private ForgotPasswordCommandHandler _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _identityProviderService = Substitute.For<IIdentityProviderService>();
        _sut = new ForgotPasswordCommandHandler(_identityProviderService);
    }

    [Test]
    public async Task Handle_WhenForgotPasswordFails_ShouldReturnFailure()
    {
        ForgotPasswordCommand command = new("user@example.com");
        var error = Error.Failure("Users.ForgotPasswordFailed", "Failed to send reset email.");

        _identityProviderService.ForgotPasswordAsync(command.Email, Arg.Any<CancellationToken>())
            .Returns(Result.Failure(error));

        Result result = await _sut.Handle(command, CancellationToken.None);

        Assert.That(result.IsFailure, Is.True);
        Assert.That(result.Error, Is.EqualTo(error));
    }

    [Test]
    public async Task Handle_WhenForgotPasswordSucceeds_ShouldReturnSuccess()
    {
        ForgotPasswordCommand command = new("user@example.com");

        _identityProviderService.ForgotPasswordAsync(command.Email, Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        Result result = await _sut.Handle(command, CancellationToken.None);

        Assert.That(result.IsSuccess, Is.True);
    }
}
