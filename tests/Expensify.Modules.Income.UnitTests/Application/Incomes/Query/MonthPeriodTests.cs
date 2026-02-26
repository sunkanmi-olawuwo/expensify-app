using Expensify.Modules.Income.Application.Incomes;

namespace Expensify.Modules.Income.UnitTests.Application.Incomes.Query;

[TestFixture]
internal sealed class MonthPeriodTests
{
    [Test]
    public void Create_WhenPeriodValid_ShouldReturnBoundaries()
    {
        Expensify.Common.Domain.Result<MonthPeriod> result = MonthPeriod.Create("2026-02", 5);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Value.StartDate, Is.EqualTo(new DateOnly(2026, 2, 5)));
            Assert.That(result.Value.EndDateExclusive, Is.EqualTo(new DateOnly(2026, 3, 5)));
        }
    }

    [Test]
    public void Create_WhenPeriodInvalid_ShouldReturnFailure()
    {
        Expensify.Common.Domain.Result<MonthPeriod> result = MonthPeriod.Create("2026-13", 5);

        Assert.That(result.IsFailure, Is.True);
    }
}
