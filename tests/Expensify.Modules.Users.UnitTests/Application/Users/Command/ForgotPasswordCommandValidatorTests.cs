using Expensify.Modules.Users.Application.Users.Command.ForgotPassword;

namespace Expensify.Modules.Users.UnitTests.Application.Users.Command;

[TestFixture]
internal sealed class ForgotPasswordCommandValidatorTests
{
    private ForgotPasswordCommandValidator _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _sut = new ForgotPasswordCommandValidator();
    }

    [Test]
    public void Validate_WhenEmailIsValid_ShouldSucceed()
    {
        ForgotPasswordCommand command = new("user@example.com");

        FluentValidation.Results.ValidationResult result = _sut.Validate(command);

        Assert.That(result.IsValid, Is.True);
    }

    [Test]
    public void Validate_WhenEmailIsInvalid_ShouldFail()
    {
        ForgotPasswordCommand command = new("not-an-email");

        FluentValidation.Results.ValidationResult result = _sut.Validate(command);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors.Any(e => e.PropertyName == nameof(ForgotPasswordCommand.Email)), Is.True);
    }

    [Test]
    public void Validate_WhenEmailIsEmpty_ShouldFail()
    {
        ForgotPasswordCommand command = new(string.Empty);

        FluentValidation.Results.ValidationResult result = _sut.Validate(command);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors.Any(e => e.PropertyName == nameof(ForgotPasswordCommand.Email)), Is.True);
    }
}
