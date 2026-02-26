using Expensify.Modules.Income.Application.Incomes.Command.CreateIncome;
using Expensify.Modules.Income.Domain.Incomes;

namespace Expensify.Modules.Income.UnitTests.Application.Incomes.Command;

[TestFixture]
internal sealed class CreateIncomeCommandValidatorTests
{
    [Test]
    public void Validate_WhenAmountIsZero_ShouldFail()
    {
        CreateIncomeCommandValidator validator = new();
        CreateIncomeCommand command = new(Guid.NewGuid(), 0m, "USD", DateOnly.FromDateTime(DateTime.UtcNow), "ACME", IncomeType.Salary, "note");

        FluentValidation.Results.ValidationResult result = validator.Validate(command);

        Assert.That(result.IsValid, Is.False);
    }
}
