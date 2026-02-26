using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Expensify.Common.Application.Data;
using Expensify.Common.Domain;
using Expensify.Modules.Users.Application.Abstractions.Identity;
using Expensify.Modules.Users.Application.Admin.Command.DeleteUser;
using Expensify.Modules.Users.Domain.Users;

namespace Expensify.Modules.Users.UnitTests.Application.Admin.Command;

[TestFixture]
internal sealed class DeleteUserCommandHandlerTests
{
    private IIdentityProviderService _identityProviderService;
    private IUserRepository _userRepository;
    private IUnitOfWork _unitOfWork;
    private ILogger<DeleteUserCommandHandler> _logger;
    private DeleteUserCommandHandler _sut;

    [SetUp]
    public void SetUp()
    {
        _identityProviderService = Substitute.For<IIdentityProviderService>();
        _userRepository = Substitute.For<IUserRepository>();
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _logger = NullLogger<DeleteUserCommandHandler>.Instance;

        _sut = new DeleteUserCommandHandler(
            _identityProviderService,
            _userRepository,
            _unitOfWork,
            _logger);
    }

    [Test]
    public async Task Handle_WhenUserNotFound_ShouldReturnNotFoundFailure()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var command = new DeleteUserCommand(userId);

        _userRepository.GetByIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns((User?)null);

        // Act
        Result result = await _sut.Handle(command, CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            // Assert
            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error, Is.EqualTo(UserErrors.NotFound(userId)));
        }
    }

    [Test]
    public async Task Handle_WhenIdentityProviderDeleteFails_ShouldReturnFailure()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var command = new DeleteUserCommand(userId);
        var user = User.Create("John", "Doe", "identity-123");

        _userRepository.GetByIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns(user);

        var identityError = Error.Failure("Identity.DeleteFailed", "Could not delete user");
        _identityProviderService.DeleteUserAsync(user.IdentityId, Arg.Any<CancellationToken>())
            .Returns(Result.Failure(identityError));

        // Act
        Result result = await _sut.Handle(command, CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            // Assert
            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error, Is.EqualTo(identityError));
        }
        _userRepository.DidNotReceive().Remove(Arg.Any<User>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WhenDeleteSucceeds_ShouldDeleteUserAndSaveChanges()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var command = new DeleteUserCommand(userId);
        var user = User.Create("John", "Doe", "identity-123");

        _userRepository.GetByIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns(user);

        _identityProviderService.DeleteUserAsync(user.IdentityId, Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        // Act
        Result result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        Assert.That(result.IsSuccess, Is.True);
        _userRepository.Received(1).Remove(user);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
