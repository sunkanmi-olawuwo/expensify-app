using NSubstitute;
using Expensify.Common.Application.Data;
using Expensify.Common.Domain;
using Expensify.Modules.Users.Application.Users.Command.UpdateUser;
using Expensify.Modules.Users.Domain.Users;

namespace Expensify.Modules.Users.UnitTests.Application.Users.Command;

[TestFixture]
internal sealed class UpdateUserCommandHandlerTests
{
    private IUserRepository _userRepository;
    private IUnitOfWork _unitOfWork;
    private UpdateUserCommandHandler _sut;

    [SetUp]
    public void SetUp()
    {
        _userRepository = Substitute.For<IUserRepository>();
        _unitOfWork = Substitute.For<IUnitOfWork>();

        _sut = new UpdateUserCommandHandler(_userRepository, _unitOfWork);
    }

    [Test]
    public async Task Handle_WhenUserNotFound_ShouldReturnNotFoundFailure()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var command = new UpdateUserCommand(userId, "Jane", "Smith");

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
    public async Task Handle_WhenUserNotFound_ShouldNotCallUpdateOrSave()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var command = new UpdateUserCommand(userId, "Jane", "Smith");

        _userRepository.GetByIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns((User?)null);

        // Act
        await _sut.Handle(command, CancellationToken.None);

        // Assert
        _userRepository.DidNotReceive().Update(Arg.Any<User>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WhenUserExists_ShouldUpdateAndSaveChanges()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var command = new UpdateUserCommand(userId, "Jane", "Smith");
        var user = User.Create("John", "Doe", "identity-123");

        _userRepository.GetByIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns(user);

        // Act
        Result result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        Assert.That(result.IsSuccess, Is.True);
        _userRepository.Received(1).Update(user);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WhenUserExists_ShouldUpdateUserProperties()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var command = new UpdateUserCommand(userId, "Jane", "Smith");
        var user = User.Create("John", "Doe", "identity-123");

        _userRepository.GetByIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns(user);

        // Act
        await _sut.Handle(command, CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            // Assert
            Assert.That(user.FirstName, Is.EqualTo("Jane"));
            Assert.That(user.LastName, Is.EqualTo("Smith"));
        }
    }
}
