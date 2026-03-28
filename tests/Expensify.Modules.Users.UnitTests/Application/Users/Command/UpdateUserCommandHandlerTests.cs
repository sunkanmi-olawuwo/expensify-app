using Expensify.Common.Application.Data;
using Expensify.Common.Domain;
using Expensify.Modules.Users.Application.Abstractions.Preferences;
using Expensify.Modules.Users.Application.Users.Command.UpdateUser;
using Expensify.Modules.Users.Domain.Preferences;
using Expensify.Modules.Users.Domain.Users;
using NSubstitute;

namespace Expensify.Modules.Users.UnitTests.Application.Users.Command;

[TestFixture]
internal sealed class UpdateUserCommandHandlerTests
{
    private IUserRepository _userRepository = null!;
    private IUserPreferenceCatalogService _userPreferenceCatalogService = null!;
    private IUnitOfWork _unitOfWork = null!;
    private UpdateUserCommandHandler _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _userRepository = Substitute.For<IUserRepository>();
        _userPreferenceCatalogService = Substitute.For<IUserPreferenceCatalogService>();
        _unitOfWork = Substitute.For<IUnitOfWork>();

        _userPreferenceCatalogService.ValidateSelectionsAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string?>(),
                Arg.Any<string?>(),
                Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        _sut = new UpdateUserCommandHandler(_userRepository, _userPreferenceCatalogService, _unitOfWork);
    }

    [Test]
    public async Task Handle_WhenUserNotFound_ShouldReturnNotFoundFailure()
    {
        var userId = Guid.NewGuid();
        var command = new UpdateUserCommand(userId, "Jane", "Smith", "USD", "UTC", 1);

        _userRepository.GetByIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns((User?)null);

        Result result = await _sut.Handle(command, CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error, Is.EqualTo(UserErrors.NotFound(userId)));
        }
    }

    [Test]
    public async Task Handle_WhenUserNotFound_ShouldNotCallUpdateOrSave()
    {
        var userId = Guid.NewGuid();
        var command = new UpdateUserCommand(userId, "Jane", "Smith", "USD", "UTC", 1);

        _userRepository.GetByIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns((User?)null);

        await _sut.Handle(command, CancellationToken.None);

        _userRepository.DidNotReceive().Update(Arg.Any<User>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WhenPreferencesAreInvalid_ShouldReturnFailure()
    {
        var userId = Guid.NewGuid();
        var command = new UpdateUserCommand(userId, "Jane", "Smith", "EUR", "America/New_York", 5);
        var user = User.Create("John", "Doe", "identity-123");
        Error error = PreferenceCatalogErrors.CurrencyNotAllowed("EUR");

        _userRepository.GetByIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns(user);
        _userPreferenceCatalogService.ValidateSelectionsAsync(
                command.Currency,
                command.Timezone,
                user.Currency,
                user.Timezone,
                Arg.Any<CancellationToken>())
            .Returns(Result.Failure(error));

        Result result = await _sut.Handle(command, CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error, Is.EqualTo(error));
        }

        _userRepository.DidNotReceive().Update(Arg.Any<User>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WhenUserExists_ShouldUpdateAndSaveChanges()
    {
        var userId = Guid.NewGuid();
        var command = new UpdateUserCommand(userId, "Jane", "Smith", "USD", "UTC", 1);
        var user = User.Create("John", "Doe", "identity-123");

        _userRepository.GetByIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns(user);

        Result result = await _sut.Handle(command, CancellationToken.None);

        Assert.That(result.IsSuccess, Is.True);
        _userRepository.Received(1).Update(user);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WhenCurrentInactivePreferenceIsResubmitted_ShouldAllowUpdate()
    {
        var userId = Guid.NewGuid();
        var command = new UpdateUserCommand(userId, "Jane", "Smith", "GBP", "UTC", 10);
        var user = User.Create("John", "Doe", "identity-123", "GBP", "UTC");

        _userRepository.GetByIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns(user);

        Result result = await _sut.Handle(command, CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(user.Currency, Is.EqualTo("GBP"));
            Assert.That(user.Timezone, Is.EqualTo("UTC"));
        }

        await _userPreferenceCatalogService.Received(1).ValidateSelectionsAsync(
            "GBP",
            "UTC",
            "GBP",
            "UTC",
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WhenUserExists_ShouldUpdateUserProperties()
    {
        var userId = Guid.NewGuid();
        var command = new UpdateUserCommand(userId, "Jane", "Smith", "EUR", "America/New_York", 5);
        var user = User.Create("John", "Doe", "identity-123");

        _userRepository.GetByIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns(user);

        await _sut.Handle(command, CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(user.FirstName, Is.EqualTo("Jane"));
            Assert.That(user.LastName, Is.EqualTo("Smith"));
            Assert.That(user.Currency, Is.EqualTo("EUR"));
            Assert.That(user.Timezone, Is.EqualTo("America/New_York"));
            Assert.That(user.MonthStartDay, Is.EqualTo(5));
        }
    }
}
