using Expensify.Modules.Users.Application.Users.Command.UpdateUser;

namespace Expensify.Modules.Users.UnitTests.Application.Users.Command;

[TestFixture]
internal sealed class UpdateUserCommandValidatorTests
{
    private UpdateUserCommandValidator _sut;

    [SetUp]
    public void SetUp()
    {
        _sut = new UpdateUserCommandValidator();
    }

    [Test]
    public void Validate_WhenCommandIsValid_ShouldSucceed()
    {
        var command = new UpdateUserCommand(
            Guid.NewGuid(),
            "Jane",
            "Doe",
            "USD",
            "America/New_York",
            5);

        FluentValidation.Results.ValidationResult result = _sut.Validate(command);

        Assert.That(result.IsValid, Is.True);
    }

    [Test]
    public void Validate_WhenCurrencyIsNotUppercaseThreeLetters_ShouldFail()
    {
        var command = new UpdateUserCommand(
            Guid.NewGuid(),
            "Jane",
            "Doe",
            "usd",
            "America/New_York",
            5);

        FluentValidation.Results.ValidationResult result = _sut.Validate(command);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors.Any(e => e.PropertyName == nameof(UpdateUserCommand.Currency)), Is.True);
    }

    [Test]
    public void Validate_WhenTimezoneIsEmpty_ShouldFail()
    {
        var command = new UpdateUserCommand(
            Guid.NewGuid(),
            "Jane",
            "Doe",
            "USD",
            string.Empty,
            5);

        FluentValidation.Results.ValidationResult result = _sut.Validate(command);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors.Any(e => e.PropertyName == nameof(UpdateUserCommand.Timezone)), Is.True);
    }

    [Test]
    public void Validate_WhenMonthStartDayOutOfRange_ShouldFail()
    {
        var command = new UpdateUserCommand(
            Guid.NewGuid(),
            "Jane",
            "Doe",
            "USD",
            "America/New_York",
            30);

        FluentValidation.Results.ValidationResult result = _sut.Validate(command);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors.Any(e => e.PropertyName == nameof(UpdateUserCommand.MonthStartDay)), Is.True);
    }
}
