using System.Data;
using System.Globalization;
using Dapper;
using Expensify.Common.Application.Clock;
using Expensify.Common.Application.Data;
using Expensify.Common.Domain;
using Expensify.Modules.Dashboard.Application.Dashboard;
using Expensify.Modules.Dashboard.Application.Dashboard.GetDashboardSummary;
using Microsoft.Data.Sqlite;
using NSubstitute;

namespace Expensify.Modules.Dashboard.UnitTests.Dashboard;

[TestFixture]
internal sealed class GetDashboardSummaryQueryHandlerTests
{
    private static readonly string[] ExpectedHistoryMonths = ["Sep 2025", "Oct 2025", "Nov 2025", "Dec 2025", "Jan 2026", "Feb 2026"];
    private static readonly decimal[] ExpectedHistoryIncome = [0m, 0m, 300m, 0m, 1000m, 1500m];
    private static readonly decimal[] ExpectedHistoryExpenses = [0m, 90m, 0m, 0m, 100m, 240m];
    private static readonly string[] ExpectedRecentTransactionTypes = ["expense", "income", "expense", "expense", "income"];
    private static readonly string[] PostedStatus = ["posted"];

    private IDbConnectionFactory _dbConnectionFactory = null!;
    private IDateTimeProvider _dateTimeProvider = null!;
    private GetDashboardSummaryQueryHandler _sut = null!;
    private SqliteConnection _connection = null!;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        SqlMapper.RemoveTypeMap(typeof(Guid));
        SqlMapper.AddTypeHandler(new GuidTypeHandler());
        SqlMapper.AddTypeHandler(new DateOnlyTypeHandler());
    }

    [SetUp]
    public void SetUp()
    {
        _dbConnectionFactory = Substitute.For<IDbConnectionFactory>();
        _dateTimeProvider = Substitute.For<IDateTimeProvider>();

        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        using SqliteCommand attachCommand = _connection.CreateCommand();
        attachCommand.CommandText =
            """
            ATTACH DATABASE ':memory:' AS users;
            ATTACH DATABASE ':memory:' AS expenses;
            ATTACH DATABASE ':memory:' AS income;
            """;
        attachCommand.ExecuteNonQuery();

        using SqliteCommand createTablesCommand = _connection.CreateCommand();
        createTablesCommand.CommandText =
            """
            CREATE TABLE users.users (
                id TEXT PRIMARY KEY,
                currency TEXT NOT NULL,
                timezone TEXT NOT NULL,
                month_start_day INTEGER NOT NULL
            );

            CREATE TABLE expenses.expense_categories (
                id TEXT PRIMARY KEY,
                name TEXT NOT NULL
            );

            CREATE TABLE expenses.expenses (
                id TEXT PRIMARY KEY,
                user_id TEXT NOT NULL,
                amount REAL NOT NULL,
                expense_date TEXT NOT NULL,
                created_at_utc TEXT NOT NULL,
                merchant TEXT NOT NULL,
                category_id TEXT NOT NULL,
                deleted_at_utc TEXT NULL
            );

            CREATE TABLE income.incomes (
                id TEXT PRIMARY KEY,
                user_id TEXT NOT NULL,
                amount REAL NOT NULL,
                income_date TEXT NOT NULL,
                created_at_utc TEXT NOT NULL,
                source TEXT NOT NULL,
                type TEXT NOT NULL,
                deleted_at_utc TEXT NULL
            );
            """;
        createTablesCommand.ExecuteNonQuery();

#pragma warning disable CA2012
        _dbConnectionFactory.OpenConnectionAsync()
            .Returns(new ValueTask<System.Data.Common.DbConnection>(_connection));
#pragma warning restore CA2012

        _sut = new GetDashboardSummaryQueryHandler(_dbConnectionFactory, _dateTimeProvider);
    }

    [TearDown]
    public void TearDown()
    {
        _connection.Dispose();
    }

    [Test]
    public async Task Handle_WhenNoDataExists_ShouldReturnZeroedDashboard()
    {
        var userId = Guid.NewGuid();
        InsertUser(userId, "GBP", "UTC", 1);
        _dateTimeProvider.UtcNow.Returns(new DateTime(2026, 3, 28, 12, 0, 0, DateTimeKind.Utc));

        Result<DashboardSummaryResponse> result = await _sut.Handle(new GetDashboardSummaryQuery(userId), CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Value.MonthlyIncome.TotalAmount, Is.EqualTo(0m));
            Assert.That(result.Value.MonthlyIncome.ChangePercentage, Is.EqualTo(0m));
            Assert.That(result.Value.MonthlyExpenses.TotalAmount, Is.EqualTo(0m));
            Assert.That(result.Value.NetCashFlow.TotalAmount, Is.EqualTo(0m));
            Assert.That(result.Value.SpendingBreakdown, Is.Empty);
            Assert.That(result.Value.RecentTransactions, Is.Empty);
            Assert.That(result.Value.MonthlyPerformance, Has.Count.EqualTo(6));
            Assert.That(result.Value.MonthlyPerformance.All(item => item.Income == 0m && item.Expenses == 0m), Is.True);
            Assert.That(result.Value.MonthlyPerformance.Last().Month, Is.EqualTo("Mar 2026"));
        }
    }

    [Test]
    public async Task Handle_ShouldAggregateDashboardSummaryAcrossPeriodsAndLimitRecentTransactions()
    {
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var foodCategoryId = Guid.NewGuid();
        var travelCategoryId = Guid.NewGuid();
        var housingCategoryId = Guid.NewGuid();

        InsertUser(userId, "GBP", "UTC", 5);
        InsertUser(otherUserId, "USD", "UTC", 1);
        InsertCategory(foodCategoryId, "Food");
        InsertCategory(travelCategoryId, "Travel");
        InsertCategory(housingCategoryId, "Housing");

        Guid currentExpenseFoodId = InsertExpense(userId, foodCategoryId, 120m, "2026-02-10", "Tesco", "2026-03-01T08:00:00.0000000Z");
        Guid currentExpenseTravelId = InsertExpense(userId, travelCategoryId, 80m, "2026-03-01", "Trainline", "2026-03-02T09:00:00.0000000Z");
        Guid currentExpenseCoffeeId = InsertExpense(userId, foodCategoryId, 40m, "2026-03-04", "Cafe", "2026-03-04T09:00:00.0000000Z");
        _ = InsertExpense(userId, foodCategoryId, 100m, "2026-01-20", "Tesco", "2026-02-02T09:00:00.0000000Z");
        _ = InsertExpense(userId, housingCategoryId, 90m, "2025-10-15", "Landlord", "2025-10-15T10:00:00.0000000Z");
        _ = InsertExpense(
            userId,
            travelCategoryId,
            999m,
            "2026-02-11",
            "Deleted Merchant",
            "2026-03-04T10:00:00.0000000Z",
            deletedAtUtc: "2026-03-04T12:00:00.0000000Z");
        _ = InsertExpense(otherUserId, foodCategoryId, 200m, "2026-02-12", "Other User Merchant", "2026-03-04T12:00:00.0000000Z");

        Guid currentIncomePayrollId = InsertIncome(userId, 1000m, "2026-02-06", "Payroll", "Salary", "2026-02-28T12:00:00.0000000Z");
        Guid currentIncomeFreelanceId = InsertIncome(userId, 500m, "2026-03-02", "Client A", "Freelance", "2026-03-04T08:00:00.0000000Z");
        _ = InsertIncome(userId, 1000m, "2026-01-10", "Payroll", "Salary", "2026-02-01T08:00:00.0000000Z");
        _ = InsertIncome(userId, 300m, "2025-11-06", "Bonus Pool", "Bonus", "2025-11-06T08:00:00.0000000Z");
        _ = InsertIncome(
            userId,
            999m,
            "2026-02-07",
            "Deleted Source",
            "Bonus",
            "2026-03-04T11:00:00.0000000Z",
            deletedAtUtc: "2026-03-04T12:30:00.0000000Z");
        _ = InsertIncome(otherUserId, 888m, "2026-02-07", "Other User Source", "Salary", "2026-03-04T13:00:00.0000000Z");

        _dateTimeProvider.UtcNow.Returns(new DateTime(2026, 3, 3, 12, 0, 0, DateTimeKind.Utc));

        Result<DashboardSummaryResponse> result = await _sut.Handle(new GetDashboardSummaryQuery(userId), CancellationToken.None);

        DashboardSummaryResponse response = result.Value;
        DashboardSpendingBreakdownItemResponse[] breakdown = response.SpendingBreakdown.ToArray();
        DashboardMonthlyPerformanceItemResponse[] performance = response.MonthlyPerformance.ToArray();
        DashboardRecentTransactionResponse[] recentTransactions = response.RecentTransactions.ToArray();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(response.MonthlyIncome.TotalAmount, Is.EqualTo(1500m));
            Assert.That(response.MonthlyIncome.Currency, Is.EqualTo("GBP"));
            Assert.That(response.MonthlyIncome.ChangePercentage, Is.EqualTo(50m));
            Assert.That(response.MonthlyExpenses.TotalAmount, Is.EqualTo(240m));
            Assert.That(response.MonthlyExpenses.ChangePercentage, Is.EqualTo(140m));
            Assert.That(response.NetCashFlow.TotalAmount, Is.EqualTo(1260m));
            Assert.That(response.NetCashFlow.ChangePercentage, Is.EqualTo(40m));
            Assert.That(breakdown, Has.Length.EqualTo(2));
            Assert.That(breakdown[0], Is.EqualTo(new DashboardSpendingBreakdownItemResponse("Food", 160m, 66.67m, "chart-1")));
            Assert.That(breakdown[1], Is.EqualTo(new DashboardSpendingBreakdownItemResponse("Travel", 80m, 33.33m, "chart-2")));
            Assert.That(performance.Select(item => item.Month), Is.EqualTo(ExpectedHistoryMonths));
            Assert.That(performance.Select(item => item.Income), Is.EqualTo(ExpectedHistoryIncome));
            Assert.That(performance.Select(item => item.Expenses), Is.EqualTo(ExpectedHistoryExpenses));
            Assert.That(recentTransactions, Has.Length.EqualTo(5));
            Assert.That(recentTransactions.Select(item => item.Id), Is.EqualTo(new[]
            {
                currentExpenseCoffeeId,
                currentIncomeFreelanceId,
                currentExpenseTravelId,
                currentExpenseFoodId,
                currentIncomePayrollId
            }));
            Assert.That(recentTransactions.Select(item => item.Type), Is.EqualTo(ExpectedRecentTransactionTypes));
            Assert.That(recentTransactions.Select(item => item.Status).Distinct(), Is.EqualTo(PostedStatus));
            Assert.That(recentTransactions.All(item => item.Timestamp.Offset == TimeSpan.Zero), Is.True);
        }
    }

    [Test]
    public async Task Handle_WhenPreviousPeriodTotalsAreZero_ShouldReturnZeroChangePercentages()
    {
        var userId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();

        InsertUser(userId, "GBP", "UTC", 1);
        InsertCategory(categoryId, "Food");
        _ = InsertExpense(userId, categoryId, 200m, "2026-03-10", "Tesco", "2026-03-10T08:00:00.0000000Z");
        _ = InsertIncome(userId, 800m, "2026-03-11", "Payroll", "Salary", "2026-03-11T08:00:00.0000000Z");

        _dateTimeProvider.UtcNow.Returns(new DateTime(2026, 3, 28, 12, 0, 0, DateTimeKind.Utc));

        Result<DashboardSummaryResponse> result = await _sut.Handle(new GetDashboardSummaryQuery(userId), CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Value.MonthlyIncome.ChangePercentage, Is.EqualTo(0m));
            Assert.That(result.Value.MonthlyExpenses.ChangePercentage, Is.EqualTo(0m));
            Assert.That(result.Value.NetCashFlow.ChangePercentage, Is.EqualTo(0m));
        }
    }

    [Test]
    public async Task Handle_ShouldRespectUserMonthBoundaryWhenSelectingCurrentPeriod()
    {
        var userId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();

        InsertUser(userId, "GBP", "UTC", 5);
        InsertCategory(categoryId, "Food");
        _ = InsertExpense(userId, categoryId, 30m, "2026-03-04", "Cafe", "2026-03-04T08:00:00.0000000Z");
        _ = InsertIncome(userId, 100m, "2026-02-05", "Payroll", "Salary", "2026-02-05T08:00:00.0000000Z");
        _ = InsertIncome(userId, 70m, "2026-02-04", "Payroll", "Salary", "2026-02-04T08:00:00.0000000Z");
        _ = InsertIncome(userId, 500m, "2026-03-05", "Payroll", "Salary", "2026-03-05T08:00:00.0000000Z");

        _dateTimeProvider.UtcNow.Returns(new DateTime(2026, 3, 3, 9, 0, 0, DateTimeKind.Utc));

        Result<DashboardSummaryResponse> result = await _sut.Handle(new GetDashboardSummaryQuery(userId), CancellationToken.None);

        DashboardMonthlyPerformanceItemResponse[] monthlyPerformance = result.Value.MonthlyPerformance.ToArray();
        DashboardMonthlyPerformanceItemResponse currentBucket = monthlyPerformance[^1];
        DashboardMonthlyPerformanceItemResponse previousBucket = monthlyPerformance[^2];

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Value.MonthlyIncome.TotalAmount, Is.EqualTo(100m));
            Assert.That(result.Value.MonthlyIncome.ChangePercentage, Is.EqualTo(42.86m));
            Assert.That(result.Value.MonthlyExpenses.TotalAmount, Is.EqualTo(30m));
            Assert.That(result.Value.NetCashFlow.TotalAmount, Is.EqualTo(70m));
            Assert.That(currentBucket.Month, Is.EqualTo("Feb 2026"));
            Assert.That(currentBucket.Income, Is.EqualTo(100m));
            Assert.That(currentBucket.Expenses, Is.EqualTo(30m));
            Assert.That(previousBucket.Month, Is.EqualTo("Jan 2026"));
            Assert.That(previousBucket.Income, Is.EqualTo(70m));
            Assert.That(previousBucket.Expenses, Is.EqualTo(0m));
        }
    }

    [Test]
    public void CalculateChangePercentage_WhenPreviousIsNegative_ShouldUseAbsoluteValue()
    {
        decimal result = DashboardCalculations.CalculateChangePercentage(-50m, -100m);

        Assert.That(result, Is.EqualTo(50m));
    }

    private void InsertUser(Guid userId, string currency, string timezone, int monthStartDay)
    {
        using SqliteCommand command = _connection.CreateCommand();
        command.CommandText =
            """
            INSERT INTO users.users (id, currency, timezone, month_start_day)
            VALUES (@id, @currency, @timezone, @monthStartDay);
            """;
        command.Parameters.AddWithValue("@id", userId.ToString());
        command.Parameters.AddWithValue("@currency", currency);
        command.Parameters.AddWithValue("@timezone", timezone);
        command.Parameters.AddWithValue("@monthStartDay", monthStartDay);
        command.ExecuteNonQuery();
    }

    private void InsertCategory(Guid categoryId, string name)
    {
        using SqliteCommand command = _connection.CreateCommand();
        command.CommandText =
            """
            INSERT INTO expenses.expense_categories (id, name)
            VALUES (@id, @name);
            """;
        command.Parameters.AddWithValue("@id", categoryId.ToString());
        command.Parameters.AddWithValue("@name", name);
        command.ExecuteNonQuery();
    }

    private Guid InsertExpense(
        Guid userId,
        Guid categoryId,
        decimal amount,
        string expenseDate,
        string merchant,
        string createdAtUtc,
        string? deletedAtUtc = null)
    {
        var expenseId = Guid.NewGuid();

        using SqliteCommand command = _connection.CreateCommand();
        command.CommandText =
            """
            INSERT INTO expenses.expenses (
                id, user_id, amount, expense_date, created_at_utc, merchant, category_id, deleted_at_utc
            )
            VALUES (
                @id, @userId, @amount, @expenseDate, @createdAtUtc, @merchant, @categoryId, @deletedAtUtc
            );
            """;
        command.Parameters.AddWithValue("@id", expenseId.ToString());
        command.Parameters.AddWithValue("@userId", userId.ToString());
        command.Parameters.AddWithValue("@amount", amount);
        command.Parameters.AddWithValue("@expenseDate", expenseDate);
        command.Parameters.AddWithValue("@createdAtUtc", createdAtUtc);
        command.Parameters.AddWithValue("@merchant", merchant);
        command.Parameters.AddWithValue("@categoryId", categoryId.ToString());
        command.Parameters.AddWithValue("@deletedAtUtc", (object?)deletedAtUtc ?? DBNull.Value);
        command.ExecuteNonQuery();

        return expenseId;
    }

    private Guid InsertIncome(
        Guid userId,
        decimal amount,
        string incomeDate,
        string source,
        string type,
        string createdAtUtc,
        string? deletedAtUtc = null)
    {
        var incomeId = Guid.NewGuid();

        using SqliteCommand command = _connection.CreateCommand();
        command.CommandText =
            """
            INSERT INTO income.incomes (
                id, user_id, amount, income_date, created_at_utc, source, type, deleted_at_utc
            )
            VALUES (
                @id, @userId, @amount, @incomeDate, @createdAtUtc, @source, @type, @deletedAtUtc
            );
            """;
        command.Parameters.AddWithValue("@id", incomeId.ToString());
        command.Parameters.AddWithValue("@userId", userId.ToString());
        command.Parameters.AddWithValue("@amount", amount);
        command.Parameters.AddWithValue("@incomeDate", incomeDate);
        command.Parameters.AddWithValue("@createdAtUtc", createdAtUtc);
        command.Parameters.AddWithValue("@source", source);
        command.Parameters.AddWithValue("@type", type);
        command.Parameters.AddWithValue("@deletedAtUtc", (object?)deletedAtUtc ?? DBNull.Value);
        command.ExecuteNonQuery();

        return incomeId;
    }

    private sealed class GuidTypeHandler : SqlMapper.TypeHandler<Guid>
    {
        public override Guid Parse(object value) => Guid.Parse((string)value);

        public override void SetValue(IDbDataParameter parameter, Guid value)
        {
            parameter.Value = value.ToString();
            parameter.DbType = DbType.String;
        }
    }

    private sealed class DateOnlyTypeHandler : SqlMapper.TypeHandler<DateOnly>
    {
        public override DateOnly Parse(object value) =>
            value switch
            {
                string date => DateOnly.ParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture),
                DateTime date => DateOnly.FromDateTime(date),
                _ => DateOnly.FromDateTime(Convert.ToDateTime(value, CultureInfo.InvariantCulture))
            };

        public override void SetValue(IDbDataParameter parameter, DateOnly value)
        {
            parameter.Value = value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            parameter.DbType = DbType.String;
        }
    }
}
