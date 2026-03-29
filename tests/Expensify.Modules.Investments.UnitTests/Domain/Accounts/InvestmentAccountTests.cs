using Expensify.Common.Domain;
using Expensify.Modules.Investments.Domain.Accounts;
using Expensify.Modules.Investments.Domain.Categories;
using InvestmentAccountEntity = Expensify.Modules.Investments.Domain.Accounts.InvestmentAccount;

namespace Expensify.Modules.Investments.UnitTests.Domain.Accounts;

[TestFixture]
internal sealed class InvestmentAccountTests
{
    [Test]
    public void Create_WhenFixedDepositOmitsInterestRate_ShouldFail()
    {
        Result<InvestmentAccountEntity> result = InvestmentAccountEntity.Create(
            Guid.NewGuid(),
            "Savings Bond",
            "Bank",
            Guid.NewGuid(),
            "GBP",
            null,
            DateTimeOffset.UtcNow.AddMonths(12),
            1000m,
            "note",
            InvestmentCategorySlugs.FixedDeposit,
            "GBP");

        Assert.That(result.IsFailure, Is.True);
        Assert.That(result.Error, Is.EqualTo(InvestmentAccountErrors.InterestRateRequired()));
    }

    [Test]
    public void Create_WhenCategoryDoesNotSupportInterestOrMaturity_ShouldClearBothFields()
    {
        Result<InvestmentAccountEntity> result = InvestmentAccountEntity.Create(
            Guid.NewGuid(),
            "Fund",
            "Provider",
            Guid.NewGuid(),
            "GBP",
            3.5m,
            new DateTimeOffset(2027, 4, 1, 0, 0, 0, TimeSpan.Zero),
            500m,
            "long term",
            InvestmentCategorySlugs.MutualFund,
            "GBP");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Value.InterestRate, Is.Null);
            Assert.That(result.Value.MaturityDate, Is.Null);
        }
    }
}
