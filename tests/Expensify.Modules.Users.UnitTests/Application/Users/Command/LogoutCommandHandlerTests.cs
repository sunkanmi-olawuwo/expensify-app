using Expensify.Common.Domain;
using Expensify.Modules.Users.Application.Abstractions.Identity;
using Expensify.Modules.Users.Application.Users.Command.Logout;
using NSubstitute;

namespace Expensify.Modules.Users.UnitTests.Application.Users.Command;

[TestFixture]
internal sealed class LogoutCommandHandlerTests
{
    private IIdentityProviderService _identityProviderService = null!;
    private LogoutCommandHandler _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _identityProviderService = Substitute.For<IIdentityProviderService>();
        _sut = new LogoutCommandHandler(_identityProviderService);
    }

    [Test]
    public async Task Handle_WhenLogoutFails_ShouldReturnFailure()
    {
        LogoutCommand command = new(Guid.NewGuid());
        var error = Error.Failure("Users.LogoutFailed", "Logout failed");

        _identityProviderService.LogoutAsync(command.UserId, Arg.Any<CancellationToken>())
            .Returns(Result.Failure(error));

        Result result = await _sut.Handle(command, CancellationToken.None);

        Assert.That(result.IsFailure, Is.True);
        Assert.That(result.Error, Is.EqualTo(error));
    }

    [Test]
    public async Task Handle_WhenLogoutSucceeds_ShouldReturnSuccess()
    {
        LogoutCommand command = new(Guid.NewGuid());

        _identityProviderService.LogoutAsync(command.UserId, Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        Result result = await _sut.Handle(command, CancellationToken.None);

        Assert.That(result.IsSuccess, Is.True);
    }
}
