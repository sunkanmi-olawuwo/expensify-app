using Expensify.Modules.Users.Application.Users.Command.ChangePassword;

namespace Expensify.Modules.Users.UnitTests.Application.Users.Command;

[TestFixture]
internal sealed class ChangePasswordCommandValidatorTests
{
    private ChangePasswordCommandValidator _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _sut = new ChangePasswordCommandValidator();
    }

    [Test]
    public void Validate_WhenCommandIsValid_ShouldSucceed()
    {
        ChangePasswordCommand command = new(Guid.NewGuid(), "OldPassword1!", "NewPassword1!");

        FluentValidation.Results.ValidationResult result = _sut.Validate(command);

        Assert.That(result.IsValid, Is.True);
    }

    [Test]
    public void Validate_WhenCurrentPasswordIsEmpty_ShouldFail()
    {
        ChangePasswordCommand command = new(Guid.NewGuid(), string.Empty, "NewPassword1!");

        FluentValidation.Results.ValidationResult result = _sut.Validate(command);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors.Any(e => e.PropertyName == nameof(ChangePasswordCommand.CurrentPassword)), Is.True);
    }

    [Test]
    public void Validate_WhenNewPasswordIsEmpty_ShouldFail()
    {
        ChangePasswordCommand command = new(Guid.NewGuid(), "OldPassword1!", string.Empty);

        FluentValidation.Results.ValidationResult result = _sut.Validate(command);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors.Any(e => e.PropertyName == nameof(ChangePasswordCommand.NewPassword)), Is.True);
    }
}
