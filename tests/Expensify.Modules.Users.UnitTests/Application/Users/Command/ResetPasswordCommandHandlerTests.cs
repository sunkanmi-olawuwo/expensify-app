using Expensify.Common.Domain;
using Expensify.Modules.Users.Application.Abstractions.Identity;
using Expensify.Modules.Users.Application.Users.Command.ResetPassword;
using NSubstitute;

namespace Expensify.Modules.Users.UnitTests.Application.Users.Command;

[TestFixture]
internal sealed class ResetPasswordCommandHandlerTests
{
    private IIdentityProviderService _identityProviderService = null!;
    private ResetPasswordCommandHandler _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _identityProviderService = Substitute.For<IIdentityProviderService>();
        _sut = new ResetPasswordCommandHandler(_identityProviderService);
    }

    [Test]
    public async Task Handle_WhenResetPasswordFails_ShouldReturnFailure()
    {
        ResetPasswordCommand command = new("user@example.com", "token", "NewPassword1!");
        var error = Error.Validation("Users.ResetPasswordFailed", "Invalid token.");

        _identityProviderService.ResetPasswordAsync(
                command.Email,
                command.Token,
                command.NewPassword,
                Arg.Any<CancellationToken>())
            .Returns(Result.Failure(error));

        Result result = await _sut.Handle(command, CancellationToken.None);

        Assert.That(result.IsFailure, Is.True);
        Assert.That(result.Error, Is.EqualTo(error));
    }

    [Test]
    public async Task Handle_WhenResetPasswordSucceeds_ShouldReturnSuccess()
    {
        ResetPasswordCommand command = new("user@example.com", "token", "NewPassword1!");

        _identityProviderService.ResetPasswordAsync(
                command.Email,
                command.Token,
                command.NewPassword,
                Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        Result result = await _sut.Handle(command, CancellationToken.None);

        Assert.That(result.IsSuccess, Is.True);
    }
}
