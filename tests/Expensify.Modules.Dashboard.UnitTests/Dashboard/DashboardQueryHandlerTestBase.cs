using System.Data;
using System.Globalization;
using Dapper;
using Expensify.Common.Application.Clock;
using Expensify.Common.Application.Data;
using Microsoft.Data.Sqlite;
using NSubstitute;

namespace Expensify.Modules.Dashboard.UnitTests.Dashboard;

internal abstract class DashboardQueryHandlerTestBase
{
    private SqliteConnection _connection = null!;

    protected IDbConnectionFactory DbConnectionFactory { get; private set; } = null!;

    protected IDateTimeProvider DateTimeProvider { get; private set; } = null!;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        SqlMapper.RemoveTypeMap(typeof(Guid));
        SqlMapper.AddTypeHandler(new GuidTypeHandler());
        SqlMapper.AddTypeHandler(new DateOnlyTypeHandler());
        SqlMapper.AddTypeHandler(new DateTimeOffsetTypeHandler());
    }

    [SetUp]
    public void SetUpBase()
    {
        DbConnectionFactory = Substitute.For<IDbConnectionFactory>();
        DateTimeProvider = Substitute.For<IDateTimeProvider>();

        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        using SqliteCommand attachCommand = _connection.CreateCommand();
        attachCommand.CommandText =
            """
            ATTACH DATABASE ':memory:' AS users;
            ATTACH DATABASE ':memory:' AS expenses;
            ATTACH DATABASE ':memory:' AS income;
            ATTACH DATABASE ':memory:' AS investments;
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

            CREATE TABLE investments.investment_categories (
                id TEXT PRIMARY KEY,
                name TEXT NOT NULL,
                slug TEXT NOT NULL
            );

            CREATE TABLE investments.investment_accounts (
                id TEXT PRIMARY KEY,
                user_id TEXT NOT NULL,
                category_id TEXT NOT NULL,
                current_balance REAL NOT NULL,
                deleted_at_utc TEXT NULL
            );

            CREATE TABLE investments.investment_contributions (
                id TEXT PRIMARY KEY,
                investment_id TEXT NOT NULL,
                amount REAL NOT NULL,
                date TEXT NOT NULL,
                deleted_at_utc TEXT NULL
            );
            """;
        createTablesCommand.ExecuteNonQuery();

#pragma warning disable CA2012
        DbConnectionFactory.OpenConnectionAsync()
            .Returns(new ValueTask<System.Data.Common.DbConnection>(_connection));
#pragma warning restore CA2012
    }

    [TearDown]
    public void TearDownBase()
    {
        _connection.Dispose();
    }

    protected void InsertUser(Guid userId, string currency, string timezone, int monthStartDay)
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

    protected void InsertExpenseCategory(Guid categoryId, string name)
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

    protected Guid InsertExpense(
        Guid userId,
        Guid categoryId,
        decimal amount,
        string expenseDate,
        string merchant = "Merchant",
        string createdAtUtc = "2026-03-01T00:00:00.0000000Z",
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

    protected Guid InsertIncome(
        Guid userId,
        decimal amount,
        string incomeDate,
        string source,
        string type,
        string createdAtUtc = "2026-03-01T00:00:00.0000000Z",
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

    protected void InsertInvestmentCategory(Guid categoryId, string name, string slug)
    {
        using SqliteCommand command = _connection.CreateCommand();
        command.CommandText =
            """
            INSERT INTO investments.investment_categories (id, name, slug)
            VALUES (@id, @name, @slug);
            """;
        command.Parameters.AddWithValue("@id", categoryId.ToString());
        command.Parameters.AddWithValue("@name", name);
        command.Parameters.AddWithValue("@slug", slug);
        command.ExecuteNonQuery();
    }

    protected Guid InsertInvestmentAccount(
        Guid userId,
        Guid categoryId,
        decimal currentBalance,
        string? deletedAtUtc = null)
    {
        var investmentId = Guid.NewGuid();

        using SqliteCommand command = _connection.CreateCommand();
        command.CommandText =
            """
            INSERT INTO investments.investment_accounts (id, user_id, category_id, current_balance, deleted_at_utc)
            VALUES (@id, @userId, @categoryId, @currentBalance, @deletedAtUtc);
            """;
        command.Parameters.AddWithValue("@id", investmentId.ToString());
        command.Parameters.AddWithValue("@userId", userId.ToString());
        command.Parameters.AddWithValue("@categoryId", categoryId.ToString());
        command.Parameters.AddWithValue("@currentBalance", currentBalance);
        command.Parameters.AddWithValue("@deletedAtUtc", (object?)deletedAtUtc ?? DBNull.Value);
        command.ExecuteNonQuery();

        return investmentId;
    }

    protected Guid InsertInvestmentContribution(
        Guid investmentId,
        decimal amount,
        string contributionDate,
        string? deletedAtUtc = null)
    {
        var contributionId = Guid.NewGuid();

        using SqliteCommand command = _connection.CreateCommand();
        command.CommandText =
            """
            INSERT INTO investments.investment_contributions (id, investment_id, amount, date, deleted_at_utc)
            VALUES (@id, @investmentId, @amount, @date, @deletedAtUtc);
            """;
        command.Parameters.AddWithValue("@id", contributionId.ToString());
        command.Parameters.AddWithValue("@investmentId", investmentId.ToString());
        command.Parameters.AddWithValue("@amount", amount);
        command.Parameters.AddWithValue("@date", contributionDate);
        command.Parameters.AddWithValue("@deletedAtUtc", (object?)deletedAtUtc ?? DBNull.Value);
        command.ExecuteNonQuery();

        return contributionId;
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

    private sealed class DateTimeOffsetTypeHandler : SqlMapper.TypeHandler<DateTimeOffset>
    {
        public override DateTimeOffset Parse(object value) =>
            value switch
            {
                string date => DateTimeOffset.Parse(date, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal),
                DateTime date => new DateTimeOffset(DateTime.SpecifyKind(date, DateTimeKind.Utc), TimeSpan.Zero),
                DateTimeOffset date => date,
                _ => DateTimeOffset.Parse(Convert.ToString(value, CultureInfo.InvariantCulture)!, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal)
            };

        public override void SetValue(IDbDataParameter parameter, DateTimeOffset value)
        {
            parameter.Value = value.ToString("O", CultureInfo.InvariantCulture);
            parameter.DbType = DbType.String;
        }
    }
}
