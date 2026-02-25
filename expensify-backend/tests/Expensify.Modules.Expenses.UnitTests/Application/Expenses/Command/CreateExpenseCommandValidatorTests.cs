using Expensify.Modules.Expenses.Application.Expenses.Command.CreateExpense;
using Expensify.Modules.Expenses.Domain.Expenses;

namespace Expensify.Modules.Expenses.UnitTests.Application.Expenses.Command;

[TestFixture]
internal sealed class CreateExpenseCommandValidatorTests
{
    private CreateExpenseCommandValidator _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _sut = new CreateExpenseCommandValidator();
    }

    [Test]
    public void Validate_WhenCommandIsValid_ShouldSucceed()
    {
        var command = new CreateExpenseCommand(
            Guid.NewGuid(),
            20.5m,
            "USD",
            DateOnly.FromDateTime(DateTime.UtcNow.Date),
            Guid.NewGuid(),
            "Tesco",
            "Groceries",
            PaymentMethod.Card,
            []);

        FluentValidation.Results.ValidationResult result = _sut.Validate(command);

        Assert.That(result.IsValid, Is.True);
    }

    [Test]
    public void Validate_WhenAmountIsZero_ShouldFail()
    {
        var command = new CreateExpenseCommand(
            Guid.NewGuid(),
            0m,
            "USD",
            DateOnly.FromDateTime(DateTime.UtcNow.Date),
            Guid.NewGuid(),
            "Tesco",
            "Groceries",
            PaymentMethod.Card,
            []);

        FluentValidation.Results.ValidationResult result = _sut.Validate(command);

        Assert.That(result.IsValid, Is.False);
    }
}
