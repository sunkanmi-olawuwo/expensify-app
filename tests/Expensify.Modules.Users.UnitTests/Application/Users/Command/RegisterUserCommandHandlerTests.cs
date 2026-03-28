using Expensify.Common.Application.Data;
using Expensify.Common.Domain;
using Expensify.Modules.Users.Application.Abstractions;
using Expensify.Modules.Users.Application.Abstractions.Identity;
using Expensify.Modules.Users.Application.Abstractions.Preferences;
using Expensify.Modules.Users.Application.Users.Command.RegisterUser;
using Expensify.Modules.Users.Domain.Preferences;
using Expensify.Modules.Users.Domain.Users;
using NSubstitute;

namespace Expensify.Modules.Users.UnitTests.Application.Users.Command;

[TestFixture]
internal sealed class RegisterUserCommandHandlerTests
{
    private IIdentityProviderService _identityProviderService = null!;
    private IUserPreferenceCatalogService _userPreferenceCatalogService = null!;
    private IUserRepository _userRepository = null!;
    private IUnitOfWork _unitOfWork = null!;
    private RegisterUserCommandHandler _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _identityProviderService = Substitute.For<IIdentityProviderService>();
        _userPreferenceCatalogService = Substitute.For<IUserPreferenceCatalogService>();
        _userRepository = Substitute.For<IUserRepository>();
        _unitOfWork = Substitute.For<IUnitOfWork>();

        _userPreferenceCatalogService.GetDefaultPreferencesAsync(Arg.Any<CancellationToken>())
            .Returns(new UserPreferenceDefaults("GBP", "UTC"));

        _sut = new RegisterUserCommandHandler(
            _identityProviderService,
            _userPreferenceCatalogService,
            _userRepository,
            _unitOfWork);
    }

    [Test]
    public async Task Handle_WhenDefaultPreferencesAreMissing_ShouldReturnFailure()
    {
        var command = new RegisterUserCommand("test@example.com", "Password1!", "John", "Doe", RoleType.User);
        Error error = PreferenceCatalogErrors.DefaultCurrencyRequired();

        _userPreferenceCatalogService.GetDefaultPreferencesAsync(Arg.Any<CancellationToken>())
            .Returns(Result.Failure<UserPreferenceDefaults>(error));

        Result<RegisterUserResponse> result = await _sut.Handle(command, CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error, Is.EqualTo(error));
        }

        await _identityProviderService.DidNotReceive().RegisterUserAsync(Arg.Any<RegisterUserRequest>(), Arg.Any<CancellationToken>());
        _userRepository.DidNotReceive().Add(Arg.Any<User>());
    }

    [Test]
    public async Task Handle_WhenIdentityProviderFails_ShouldReturnFailure()
    {
        var command = new RegisterUserCommand("test@example.com", "Password1!", "John", "Doe", RoleType.User);
        var identityError = Error.Failure("Identity.RegistrationFailed", "Registration failed");

        _identityProviderService.RegisterUserAsync(Arg.Any<RegisterUserRequest>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<string>(identityError));

        Result<RegisterUserResponse> result = await _sut.Handle(command, CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error, Is.EqualTo(identityError));
        }

        _userRepository.DidNotReceive().Add(Arg.Any<User>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WhenRegistrationSucceeds_ShouldInsertUserAndReturnId()
    {
        var command = new RegisterUserCommand("test@example.com", "Password1!", "John", "Doe", RoleType.User);
        const string IdentityId = "identity-abc-123";

        _identityProviderService.RegisterUserAsync(Arg.Any<RegisterUserRequest>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success<string>(IdentityId));

        Result<RegisterUserResponse> result = await _sut.Handle(command, CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Value, Is.Not.Null);
        }

        _userRepository.Received(1).Add(Arg.Is<User>(user =>
            user.FirstName == "John" &&
            user.LastName == "Doe" &&
            user.IdentityId == IdentityId &&
            user.Currency == "GBP" &&
            user.Timezone == "UTC"));
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WhenRegistrationSucceeds_ShouldPassCorrectRequestToIdentityProvider()
    {
        var command = new RegisterUserCommand("test@example.com", "Password1!", "John", "Doe", RoleType.User);

        _identityProviderService.RegisterUserAsync(Arg.Any<RegisterUserRequest>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success<string>("identity-id"));

        await _sut.Handle(command, CancellationToken.None);

        await _identityProviderService.Received(1).RegisterUserAsync(
            Arg.Is<RegisterUserRequest>(request =>
                request.Email == "test@example.com" &&
                request.Password == "Password1!" &&
                request.FirstName == "John" &&
                request.LastName == "Doe" &&
                request.Role == RoleType.User),
            Arg.Any<CancellationToken>());
    }
}
