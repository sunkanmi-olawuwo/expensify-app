using Expensify.Common.Domain;
using Expensify.Modules.Dashboard.Application.Dashboard.Analytics.GetDashboardCashFlowTrend;
using Expensify.Modules.Dashboard.Application.Dashboard.Analytics.GetDashboardCategoryComparison;
using Expensify.Modules.Dashboard.Application.Dashboard.Analytics.GetDashboardIncomeBreakdown;
using Expensify.Modules.Dashboard.Application.Dashboard.Analytics.GetDashboardInvestmentAllocation;
using Expensify.Modules.Dashboard.Application.Dashboard.Analytics.GetDashboardInvestmentTrend;
using Expensify.Modules.Dashboard.Application.Dashboard.Analytics.GetDashboardTopCategories;
using NSubstitute;

namespace Expensify.Modules.Dashboard.UnitTests.Dashboard;

[TestFixture]
internal sealed class GetDashboardCashFlowTrendQueryHandlerTests : DashboardQueryHandlerTestBase
{
    [Test]
    public async Task Handle_ShouldReturnConfiguredMonthWindowAndSavingsRates()
    {
        var userId = Guid.NewGuid();
        var foodCategoryId = Guid.NewGuid();

        InsertUser(userId, "GBP", "UTC", 1);
        InsertExpenseCategory(foodCategoryId, "Food");
        InsertIncome(userId, 1000m, "2026-01-10", "Payroll", "Salary");
        InsertIncome(userId, 1200m, "2026-02-10", "Payroll", "Salary");
        InsertIncome(userId, 800m, "2026-03-10", "Payroll", "Salary");
        InsertExpense(userId, foodCategoryId, 500m, "2026-01-11");
        InsertExpense(userId, foodCategoryId, 400m, "2026-02-11");
        InsertExpense(userId, foodCategoryId, 900m, "2026-03-11");

        DateTimeProvider.UtcNow.Returns(new DateTime(2026, 3, 28, 12, 0, 0, DateTimeKind.Utc));
        var sut = new GetDashboardCashFlowTrendQueryHandler(DbConnectionFactory, DateTimeProvider);

        Result<DashboardCashFlowTrendResponse> result =
            await sut.Handle(new GetDashboardCashFlowTrendQuery(userId, 3), CancellationToken.None);

        DashboardCashFlowTrendMonthResponse[] months = result.Value.Months.ToArray();
        string[] expectedLabels = ["Jan 2026", "Feb 2026", "Mar 2026"];
        decimal[] expectedCashFlow = [500m, 800m, -100m];
        decimal[] expectedSavingsRates = [50m, 66.67m, -12.5m];

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Value.Currency, Is.EqualTo("GBP"));
            Assert.That(months.Select(item => item.Label), Is.EqualTo(expectedLabels));
            Assert.That(months.Select(item => item.NetCashFlow), Is.EqualTo(expectedCashFlow));
            Assert.That(months.Select(item => item.SavingsRate), Is.EqualTo(expectedSavingsRates));
        }
    }

    [Test]
    public async Task Handle_WhenMonthsIsInvalid_ShouldFallBackToSixMonths()
    {
        var userId = Guid.NewGuid();

        InsertUser(userId, "GBP", "UTC", 1);
        DateTimeProvider.UtcNow.Returns(new DateTime(2026, 3, 28, 12, 0, 0, DateTimeKind.Utc));
        var sut = new GetDashboardCashFlowTrendQueryHandler(DbConnectionFactory, DateTimeProvider);

        Result<DashboardCashFlowTrendResponse> result =
            await sut.Handle(new GetDashboardCashFlowTrendQuery(userId, 99), CancellationToken.None);

        Assert.That(result.Value.Months, Has.Count.EqualTo(6));
    }
}

[TestFixture]
internal sealed class GetDashboardIncomeBreakdownQueryHandlerTests : DashboardQueryHandlerTestBase
{
    [Test]
    public async Task Handle_ShouldGroupByIncomeTypeAndReturnCorrectedPercentages()
    {
        var userId = Guid.NewGuid();

        InsertUser(userId, "GBP", "UTC", 1);
        InsertIncome(userId, 1000m, "2026-01-10", "Payroll", "Salary");
        InsertIncome(userId, 500m, "2026-02-10", "Client A", "Freelance");
        InsertIncome(userId, 1m, "2026-03-10", "Bonus Pool", "Bonus");

        DateTimeProvider.UtcNow.Returns(new DateTime(2026, 3, 28, 12, 0, 0, DateTimeKind.Utc));
        var sut = new GetDashboardIncomeBreakdownQueryHandler(DbConnectionFactory, DateTimeProvider);

        Result<DashboardIncomeBreakdownResponse> result =
            await sut.Handle(new GetDashboardIncomeBreakdownQuery(userId, 99), CancellationToken.None);

        DashboardIncomeBreakdownSourceResponse[] sources = result.Value.Sources.ToArray();
        string[] expectedSources = ["Salary", "Freelance", "Bonus"];

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Value.Period, Is.EqualTo("Last 3 months"));
            Assert.That(result.Value.TotalIncome, Is.EqualTo(1501m));
            Assert.That(sources.Select(item => item.Source), Is.EqualTo(expectedSources));
            Assert.That(sources.Sum(item => item.Percentage), Is.EqualTo(100m));
            Assert.That(sources[0].ColorKey, Is.EqualTo("chart-1"));
            Assert.That(sources[1].ColorKey, Is.EqualTo("chart-2"));
        }
    }
}

[TestFixture]
internal sealed class GetDashboardCategoryComparisonQueryHandlerTests : DashboardQueryHandlerTestBase
{
    [Test]
    public async Task Handle_ShouldRespectMonthStartDayForExplicitMonthComparison()
    {
        var userId = Guid.NewGuid();
        var foodCategoryId = Guid.NewGuid();
        var travelCategoryId = Guid.NewGuid();

        InsertUser(userId, "GBP", "UTC", 5);
        InsertExpenseCategory(foodCategoryId, "Food");
        InsertExpenseCategory(travelCategoryId, "Travel");
        InsertExpense(userId, foodCategoryId, 80m, "2026-01-10");
        InsertExpense(userId, foodCategoryId, 30m, "2026-02-04");
        InsertExpense(userId, foodCategoryId, 120m, "2026-02-10");
        InsertExpense(userId, travelCategoryId, 50m, "2026-02-20");
        InsertExpense(userId, travelCategoryId, 25m, "2026-03-04");

        DateTimeProvider.UtcNow.Returns(new DateTime(2026, 3, 3, 12, 0, 0, DateTimeKind.Utc));
        var sut = new GetDashboardCategoryComparisonQueryHandler(DbConnectionFactory, DateTimeProvider);

        Result<DashboardCategoryComparisonResponse> result =
            await sut.Handle(new GetDashboardCategoryComparisonQuery(userId, "2026-02"), CancellationToken.None);

        DashboardCategoryComparisonItemResponse[] categories = result.Value.Categories.ToArray();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Value.CurrentMonth, Is.EqualTo("Feb 2026"));
            Assert.That(result.Value.PreviousMonth, Is.EqualTo("Jan 2026"));
            Assert.That(categories[0], Is.EqualTo(new DashboardCategoryComparisonItemResponse("Food", 120m, 110m, 10m, 9.09m)));
            Assert.That(categories[1], Is.EqualTo(new DashboardCategoryComparisonItemResponse("Travel", 75m, 0m, 75m, 0m)));
        }
    }
}

[TestFixture]
internal sealed class GetDashboardTopCategoriesQueryHandlerTests : DashboardQueryHandlerTestBase
{
    [Test]
    public async Task Handle_ShouldUseTotalSpentAsPercentageBaseWhenLimitTruncatesResults()
    {
        var userId = Guid.NewGuid();
        var foodCategoryId = Guid.NewGuid();
        var travelCategoryId = Guid.NewGuid();
        var rentCategoryId = Guid.NewGuid();

        InsertUser(userId, "GBP", "UTC", 1);
        InsertExpenseCategory(foodCategoryId, "Food");
        InsertExpenseCategory(travelCategoryId, "Travel");
        InsertExpenseCategory(rentCategoryId, "Rent");
        InsertExpense(userId, rentCategoryId, 600m, "2026-03-05");
        InsertExpense(userId, foodCategoryId, 300m, "2026-03-06");
        InsertExpense(userId, travelCategoryId, 100m, "2026-03-07");

        DateTimeProvider.UtcNow.Returns(new DateTime(2026, 3, 28, 12, 0, 0, DateTimeKind.Utc));
        var sut = new GetDashboardTopCategoriesQueryHandler(DbConnectionFactory, DateTimeProvider);

        Result<DashboardTopCategoriesResponse> result =
            await sut.Handle(new GetDashboardTopCategoriesQuery(userId, 3, 1), CancellationToken.None);

        DashboardTopCategoryResponse item = result.Value.Categories.Single();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Value.TotalSpent, Is.EqualTo(1000m));
            Assert.That(item, Is.EqualTo(new DashboardTopCategoryResponse(1, "Rent", 600m, 60m, "chart-1")));
        }
    }
}

[TestFixture]
internal sealed class GetDashboardInvestmentAllocationQueryHandlerTests : DashboardQueryHandlerTestBase
{
    [Test]
    public async Task Handle_ShouldExcludeSoftDeletedAccountsAndReturnCategoryPercentages()
    {
        var userId = Guid.NewGuid();
        var isaCategoryId = Guid.NewGuid();
        var lisaCategoryId = Guid.NewGuid();

        InsertUser(userId, "GBP", "UTC", 1);
        InsertInvestmentCategory(isaCategoryId, "ISA", "isa");
        InsertInvestmentCategory(lisaCategoryId, "LISA", "lisa");
        InsertInvestmentAccount(userId, isaCategoryId, 1200m);
        InsertInvestmentAccount(userId, lisaCategoryId, 800m);
        InsertInvestmentAccount(userId, isaCategoryId, 999m, deletedAtUtc: "2026-03-01T00:00:00.0000000Z");

        var sut = new GetDashboardInvestmentAllocationQueryHandler(DbConnectionFactory);

        Result<DashboardInvestmentAllocationResponse> result =
            await sut.Handle(new GetDashboardInvestmentAllocationQuery(userId), CancellationToken.None);

        DashboardInvestmentAllocationCategoryResponse[] categories = result.Value.Categories.ToArray();
        string[] expectedCategories = ["ISA", "LISA"];

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Value.TotalValue, Is.EqualTo(2000m));
            Assert.That(result.Value.AccountCount, Is.EqualTo(2));
            Assert.That(categories.Select(item => item.CategoryName), Is.EqualTo(expectedCategories));
            Assert.That(categories.Sum(item => item.Percentage), Is.EqualTo(100m));
        }
    }
}

[TestFixture]
internal sealed class GetDashboardInvestmentTrendQueryHandlerTests : DashboardQueryHandlerTestBase
{
    [Test]
    public async Task Handle_ShouldBucketContributionsByUserLocalMonthAndIgnoreSoftDeletedRows()
    {
        var userId = Guid.NewGuid();
        var isaCategoryId = Guid.NewGuid();
        var lisaCategoryId = Guid.NewGuid();

        InsertUser(userId, "GBP", "Europe/Berlin", 1);
        InsertInvestmentCategory(isaCategoryId, "ISA", "isa");
        InsertInvestmentCategory(lisaCategoryId, "LISA", "lisa");
        Guid isaAccountId = InsertInvestmentAccount(userId, isaCategoryId, 1000m);
        Guid lisaAccountId = InsertInvestmentAccount(userId, lisaCategoryId, 500m);
        InsertInvestmentContribution(isaAccountId, 200m, "2026-01-15T10:00:00.0000000+00:00");
        InsertInvestmentContribution(isaAccountId, 100m, "2026-02-28T23:30:00.0000000+00:00");
        InsertInvestmentContribution(lisaAccountId, 50m, "2026-03-10T08:00:00.0000000+00:00");
        InsertInvestmentContribution(lisaAccountId, 999m, "2026-03-12T08:00:00.0000000+00:00", deletedAtUtc: "2026-03-12T10:00:00.0000000Z");

        DateTimeProvider.UtcNow.Returns(new DateTime(2026, 3, 28, 12, 0, 0, DateTimeKind.Utc));
        var sut = new GetDashboardInvestmentTrendQueryHandler(DbConnectionFactory, DateTimeProvider);

        Result<DashboardInvestmentTrendResponse> result =
            await sut.Handle(new GetDashboardInvestmentTrendQuery(userId, 3), CancellationToken.None);

        DashboardInvestmentTrendMonthResponse[] months = result.Value.Months.ToArray();
        string[] expectedLabels = ["Jan 2026", "Feb 2026", "Mar 2026"];
        decimal[] expectedContributions = [200m, 0m, 150m];

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Value.TotalContributed, Is.EqualTo(350m));
            Assert.That(months.Select(item => item.Label), Is.EqualTo(expectedLabels));
            Assert.That(months.Select(item => item.Contributions), Is.EqualTo(expectedContributions));
            Assert.That(months[2].ContributingAccountCount, Is.EqualTo(2));
        }
    }
}
