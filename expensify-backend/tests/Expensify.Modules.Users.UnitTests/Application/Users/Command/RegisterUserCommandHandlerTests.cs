using NSubstitute;
using Expensify.Common.Application.Data;
using Expensify.Common.Domain;
using Expensify.Modules.Users.Application.Abstractions;
using Expensify.Modules.Users.Application.Abstractions.Identity;
using Expensify.Modules.Users.Application.Users.Command.RegisterUser;
using Expensify.Modules.Users.Domain.Users;

namespace Expensify.Modules.Users.UnitTests.Application.Users.Command;

[TestFixture]
internal sealed class RegisterUserCommandHandlerTests
{
    private IIdentityProviderService _identityProviderService;
    private IUserRepository _userRepository;
    private IUnitOfWork _unitOfWork;
    private RegisterUserCommandHandler _sut;

    [SetUp]
    public void SetUp()
    {
        _identityProviderService = Substitute.For<IIdentityProviderService>();
        _userRepository = Substitute.For<IUserRepository>();
        _unitOfWork = Substitute.For<IUnitOfWork>();

        _sut = new RegisterUserCommandHandler(
            _identityProviderService,
            _userRepository,
            _unitOfWork);
    }

    [Test]
    public async Task Handle_WhenIdentityProviderFails_ShouldReturnFailure()
    {
        // Arrange
        var command = new RegisterUserCommand("test@example.com", "Password1!", "John", "Doe", RoleType.Parent);
        var identityError = Error.Failure("Identity.RegistrationFailed", "Registration failed");

        _identityProviderService.RegisterUserAsync(Arg.Any<RegisterUserRequest>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<string>(identityError));

        // Act
        Result<RegisterUserResponse> result = await _sut.Handle(command, CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            // Assert
            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error, Is.EqualTo(identityError));
        }
        _userRepository.DidNotReceive().Add(Arg.Any<User>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WhenIdentityProviderFails_ShouldNotInsertUser()
    {
        // Arrange
        var command = new RegisterUserCommand("test@example.com", "Password1!", "John", "Doe", RoleType.Tutor);

        _identityProviderService.RegisterUserAsync(Arg.Any<RegisterUserRequest>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<string>(Error.Failure("Identity.Error", "Error")));

        // Act
        await _sut.Handle(command, CancellationToken.None);

        // Assert
        _userRepository.DidNotReceive().Add(Arg.Any<User>());
    }

    [Test]
    public async Task Handle_WhenRegistrationSucceeds_ShouldInsertUserAndReturnId()
    {
        // Arrange
        var command = new RegisterUserCommand("test@example.com", "Password1!", "John", "Doe", RoleType.Parent);
        string identityId = "identity-abc-123";

        _identityProviderService.RegisterUserAsync(Arg.Any<RegisterUserRequest>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success<string>(identityId));

        // Act
        Result<RegisterUserResponse> result = await _sut.Handle(command, CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            // Assert
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Value, Is.Not.Null);
        }
        _userRepository.Received(1).Add(Arg.Is<User>(u =>
            u.FirstName == "John" &&
            u.LastName == "Doe" &&
            u.IdentityId == identityId));
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WhenRegistrationSucceeds_ShouldPassCorrectRequestToIdentityProvider()
    {
        // Arrange
        var command = new RegisterUserCommand("test@example.com", "Password1!", "John", "Doe", RoleType.Parent);

        _identityProviderService.RegisterUserAsync(Arg.Any<RegisterUserRequest>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success<string>("identity-id"));

        // Act
        await _sut.Handle(command, CancellationToken.None);

        // Assert
        await _identityProviderService.Received(1).RegisterUserAsync(
            Arg.Is<RegisterUserRequest>(r =>
                r.Email == "test@example.com" &&
                r.Password == "Password1!" &&
                r.FirstName == "John" &&
                r.LastName == "Doe" &&
                r.Role == RoleType.Parent),
            Arg.Any<CancellationToken>());
    }
}
