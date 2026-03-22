using Expensify.Common.Domain;
using Expensify.Modules.Users.Application.Abstractions.Identity;
using Expensify.Modules.Users.Application.Users.Command.ChangePassword;
using NSubstitute;

namespace Expensify.Modules.Users.UnitTests.Application.Users.Command;

[TestFixture]
internal sealed class ChangePasswordCommandHandlerTests
{
    private IIdentityProviderService _identityProviderService = null!;
    private ChangePasswordCommandHandler _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _identityProviderService = Substitute.For<IIdentityProviderService>();
        _sut = new ChangePasswordCommandHandler(_identityProviderService);
    }

    [Test]
    public async Task Handle_WhenChangePasswordFails_ShouldReturnFailure()
    {
        ChangePasswordCommand command = new(Guid.NewGuid(), "OldPassword1!", "NewPassword1!");
        var error = Error.Validation("Users.ChangePasswordFailed", "Current password is incorrect.");

        _identityProviderService.ChangePasswordAsync(
                command.UserId,
                command.CurrentPassword,
                command.NewPassword,
                Arg.Any<CancellationToken>())
            .Returns(Result.Failure(error));

        Result result = await _sut.Handle(command, CancellationToken.None);

        Assert.That(result.IsFailure, Is.True);
        Assert.That(result.Error, Is.EqualTo(error));
    }

    [Test]
    public async Task Handle_WhenChangePasswordSucceeds_ShouldReturnSuccess()
    {
        ChangePasswordCommand command = new(Guid.NewGuid(), "OldPassword1!", "NewPassword1!");

        _identityProviderService.ChangePasswordAsync(
                command.UserId,
                command.CurrentPassword,
                command.NewPassword,
                Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        Result result = await _sut.Handle(command, CancellationToken.None);

        Assert.That(result.IsSuccess, Is.True);
    }
}
