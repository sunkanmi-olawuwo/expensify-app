using NSubstitute;
using Expensify.Common.Domain;
using Expensify.Modules.Users.Application.Abstractions;
using Expensify.Modules.Users.Application.Abstractions.Identity;
using Expensify.Modules.Users.Application.Users.Command.RefreshToken;

namespace Expensify.Modules.Users.UnitTests.Application.Users.Command;

[TestFixture]
internal sealed class RefreshTokenCommandHandlerTests
{
    private IIdentityProviderService _identityProviderService;
    private RefreshTokenCommandHandler _sut;

    [SetUp]
    public void SetUp()
    {
        _identityProviderService = Substitute.For<IIdentityProviderService>();

        _sut = new RefreshTokenCommandHandler(_identityProviderService);
    }

    [Test]
    public async Task Handle_WhenRefreshTokenFails_ShouldReturnFailure()
    {
        // Arrange
        var command = new RefreshTokenCommand("expired-token", "old-refresh-token");
        var tokenError = Error.Failure("Identity.RefreshFailed", "Token refresh failed");

        _identityProviderService.RefreshTokenAsync("expired-token", "old-refresh-token", Arg.Any<CancellationToken>())
            .Returns(Result.Failure<RefreshTokenResponse>(tokenError));

        // Act
        Result<RefreshTokenResponse> result = await _sut.Handle(command, CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            // Assert
            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error, Is.EqualTo(tokenError));
        }
    }

    [Test]
    public async Task Handle_WhenRefreshTokenSucceeds_ShouldReturnNewTokens()
    {
        // Arrange
        var command = new RefreshTokenCommand("valid-token", "valid-refresh-token");
        var expectedResponse = new RefreshTokenResponse("new-token", "new-refresh-token");

        _identityProviderService.RefreshTokenAsync("valid-token", "valid-refresh-token", Arg.Any<CancellationToken>())
            .Returns(Result.Success(expectedResponse));

        // Act
        Result<RefreshTokenResponse> result = await _sut.Handle(command, CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            // Assert
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Value.Token, Is.EqualTo("new-token"));
            Assert.That(result.Value.RefreshToken, Is.EqualTo("new-refresh-token"));
        }
    }

    [Test]
    public async Task Handle_ShouldPassCorrectTokensToIdentityProvider()
    {
        // Arrange
        var command = new RefreshTokenCommand("my-token", "my-refresh-token");

        _identityProviderService.RefreshTokenAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success(new RefreshTokenResponse("t", "rt")));

        // Act
        await _sut.Handle(command, CancellationToken.None);

        // Assert
        await _identityProviderService.Received(1)
            .RefreshTokenAsync("my-token", "my-refresh-token", Arg.Any<CancellationToken>());
    }
}
