using Expensify.Modules.Users.Application.Users.Command.ResetPassword;

namespace Expensify.Modules.Users.UnitTests.Application.Users.Command;

[TestFixture]
internal sealed class ResetPasswordCommandValidatorTests
{
    private ResetPasswordCommandValidator _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _sut = new ResetPasswordCommandValidator();
    }

    [Test]
    public void Validate_WhenCommandIsValid_ShouldSucceed()
    {
        ResetPasswordCommand command = new("user@example.com", "token", "NewPassword1!");

        FluentValidation.Results.ValidationResult result = _sut.Validate(command);

        Assert.That(result.IsValid, Is.True);
    }

    [Test]
    public void Validate_WhenEmailIsInvalid_ShouldFail()
    {
        ResetPasswordCommand command = new("invalid-email", "token", "NewPassword1!");

        FluentValidation.Results.ValidationResult result = _sut.Validate(command);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors.Any(e => e.PropertyName == nameof(ResetPasswordCommand.Email)), Is.True);
    }

    [Test]
    public void Validate_WhenEmailIsEmpty_ShouldFail()
    {
        ResetPasswordCommand command = new(string.Empty, "token", "NewPassword1!");

        FluentValidation.Results.ValidationResult result = _sut.Validate(command);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors.Any(e => e.PropertyName == nameof(ResetPasswordCommand.Email)), Is.True);
    }

    [Test]
    public void Validate_WhenTokenIsEmpty_ShouldFail()
    {
        ResetPasswordCommand command = new("user@example.com", string.Empty, "NewPassword1!");

        FluentValidation.Results.ValidationResult result = _sut.Validate(command);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors.Any(e => e.PropertyName == nameof(ResetPasswordCommand.Token)), Is.True);
    }

    [Test]
    public void Validate_WhenNewPasswordIsEmpty_ShouldFail()
    {
        ResetPasswordCommand command = new("user@example.com", "token", string.Empty);

        FluentValidation.Results.ValidationResult result = _sut.Validate(command);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors.Any(e => e.PropertyName == nameof(ResetPasswordCommand.NewPassword)), Is.True);
    }
}
