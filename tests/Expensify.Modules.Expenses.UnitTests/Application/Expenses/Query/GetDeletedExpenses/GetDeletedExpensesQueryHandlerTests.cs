using System.Data;
using System.Globalization;
using Dapper;
using Microsoft.Data.Sqlite;
using NSubstitute;
using Expensify.Common.Application.Clock;
using Expensify.Common.Application.Data;
using Expensify.Common.Domain;
using Expensify.Modules.Expenses.Application.Abstractions;
using Expensify.Modules.Expenses.Application.Abstractions.SoftDelete;
using Expensify.Modules.Expenses.Application.Expenses.Query.GetDeletedExpenses;

namespace Expensify.Modules.Expenses.UnitTests.Application.Expenses.Query.GetDeletedExpenses;

[TestFixture]
internal sealed class GetDeletedExpensesQueryHandlerTests
{
    private IDbConnectionFactory _dbConnectionFactory = null!;
    private IDateTimeProvider _dateTimeProvider = null!;
    private ISoftDeleteRetentionProvider _retentionProvider = null!;
    private GetDeletedExpensesQueryHandler _sut = null!;
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
        _retentionProvider = Substitute.For<ISoftDeleteRetentionProvider>();
        _retentionProvider.RetentionDays.Returns(30);

        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        using SqliteCommand attachCommand = _connection.CreateCommand();
        attachCommand.CommandText = "ATTACH DATABASE ':memory:' AS expenses;";
        attachCommand.ExecuteNonQuery();

        using SqliteCommand createTablesCommand = _connection.CreateCommand();
        createTablesCommand.CommandText =
            """
            CREATE TABLE expenses.expense_categories (
                id TEXT PRIMARY KEY,
                name TEXT NOT NULL
            );

            CREATE TABLE expenses.expenses (
                id TEXT PRIMARY KEY,
                user_id TEXT NOT NULL,
                amount REAL NOT NULL,
                currency TEXT NOT NULL,
                expense_date TEXT NOT NULL,
                category_id TEXT NOT NULL,
                merchant TEXT NOT NULL,
                note TEXT NOT NULL,
                payment_method TEXT NOT NULL,
                deleted_at_utc TEXT NULL
            );

            CREATE TABLE expenses.expense_tags (
                id TEXT PRIMARY KEY,
                name TEXT NOT NULL
            );

            CREATE TABLE expenses.expense_expense_tags (
                expense_id TEXT NOT NULL,
                tags_id TEXT NOT NULL
            );
            """;
        createTablesCommand.ExecuteNonQuery();

#pragma warning disable CA2012
        _dbConnectionFactory.OpenConnectionAsync()
            .Returns(new ValueTask<System.Data.Common.DbConnection>(_connection));
#pragma warning restore CA2012

        _sut = new GetDeletedExpensesQueryHandler(_dbConnectionFactory, _dateTimeProvider, _retentionProvider);
    }

    [TearDown]
    public void TearDown()
    {
        _connection.Dispose();
    }

    [Test]
    public async Task Handle_WhenDeletedExpenseIsPastRetention_ShouldClampDaysUntilPermanentDeletionToZero()
    {
        var userId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        var expenseId = Guid.NewGuid();
        var deletedAtUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        InsertCategory(categoryId, "Food");
        InsertDeletedExpense(expenseId, userId, categoryId, deletedAtUtc);

        _dateTimeProvider.UtcNow.Returns(new DateTime(2026, 2, 20, 0, 0, 0, DateTimeKind.Utc));

        Result<DeletedExpensesPageResponse> result = await _sut.Handle(new GetDeletedExpensesQuery(userId, 1, 20), CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Value.Items, Has.Count.EqualTo(1));
            Assert.That(result.Value.Items.Single().DaysUntilPermanentDeletion, Is.EqualTo(0));
        }
    }

    [Test]
    public async Task Handle_WhenPaginationInputsAreInvalid_ShouldApplyDefaults()
    {
        var userId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        InsertCategory(categoryId, "Food");

        for (int i = 0; i < 120; i++)
        {
            InsertDeletedExpense(
                Guid.NewGuid(),
                userId,
                categoryId,
                new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc).AddMinutes(i));
        }

        _dateTimeProvider.UtcNow.Returns(new DateTime(2026, 2, 28, 0, 0, 0, DateTimeKind.Utc));

        Result<DeletedExpensesPageResponse> invalidResult = await _sut.Handle(new GetDeletedExpensesQuery(userId, 0, -5), CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(invalidResult.IsSuccess, Is.True);
            Assert.That(invalidResult.Value.Page, Is.EqualTo(1));
            Assert.That(invalidResult.Value.PageSize, Is.EqualTo(20));
            Assert.That(invalidResult.Value.Items, Has.Count.EqualTo(20));
        }
    }

    [Test]
    public async Task Handle_WhenPageSizeIsAboveMaximum_ShouldClampToMaxPageSize()
    {
        var userId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        InsertCategory(categoryId, "Food");

        for (int i = 0; i < 120; i++)
        {
            InsertDeletedExpense(
                Guid.NewGuid(),
                userId,
                categoryId,
                new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc).AddMinutes(i));
        }

        _dateTimeProvider.UtcNow.Returns(new DateTime(2026, 2, 28, 0, 0, 0, DateTimeKind.Utc));

        Result<DeletedExpensesPageResponse> result = await _sut.Handle(new GetDeletedExpensesQuery(userId, 1, 1000), CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Value.PageSize, Is.EqualTo(100));
            Assert.That(result.Value.TotalCount, Is.EqualTo(120));
            Assert.That(result.Value.TotalPages, Is.EqualTo(2));
            Assert.That(result.Value.Items, Has.Count.EqualTo(100));
        }
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

    private void InsertDeletedExpense(Guid expenseId, Guid userId, Guid categoryId, DateTime deletedAtUtc)
    {
        using SqliteCommand command = _connection.CreateCommand();
        command.CommandText =
            """
            INSERT INTO expenses.expenses (
                id, user_id, amount, currency, expense_date, category_id, merchant, note, payment_method, deleted_at_utc
            )
            VALUES (
                @id, @userId, @amount, @currency, @expenseDate, @categoryId, @merchant, @note, @paymentMethod, @deletedAtUtc
            );
            """;
        command.Parameters.AddWithValue("@id", expenseId.ToString());
        command.Parameters.AddWithValue("@userId", userId.ToString());
        command.Parameters.AddWithValue("@amount", 10m);
        command.Parameters.AddWithValue("@currency", "GBP");
        command.Parameters.AddWithValue("@expenseDate", "2026-02-10");
        command.Parameters.AddWithValue("@categoryId", categoryId.ToString());
        command.Parameters.AddWithValue("@merchant", "Tesco");
        command.Parameters.AddWithValue("@note", "Weekly");
        command.Parameters.AddWithValue("@paymentMethod", "Card");
        command.Parameters.AddWithValue("@deletedAtUtc", deletedAtUtc.ToString("O"));
        command.ExecuteNonQuery();
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
                string s => DateOnly.ParseExact(s, "yyyy-MM-dd", CultureInfo.InvariantCulture),
                DateTime dt => DateOnly.FromDateTime(dt),
                _ => DateOnly.FromDateTime(Convert.ToDateTime(value, CultureInfo.InvariantCulture))
            };

        public override void SetValue(IDbDataParameter parameter, DateOnly value)
        {
            parameter.Value = value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            parameter.DbType = DbType.String;
        }
    }
}
