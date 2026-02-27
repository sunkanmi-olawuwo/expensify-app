using System.Data;
using System.Globalization;
using Dapper;
using Microsoft.Data.Sqlite;
using NSubstitute;
using Expensify.Common.Application.Clock;
using Expensify.Common.Application.Data;
using Expensify.Common.Domain;
using Expensify.Modules.Income.Application.Abstractions;
using Expensify.Modules.Income.Application.Abstractions.SoftDelete;
using Expensify.Modules.Income.Application.Incomes.Query.GetDeletedIncome;

namespace Expensify.Modules.Income.UnitTests.Application.Incomes.Query.GetDeletedIncome;

[TestFixture]
internal sealed class GetDeletedIncomeQueryHandlerTests
{
    private IDbConnectionFactory _dbConnectionFactory = null!;
    private IDateTimeProvider _dateTimeProvider = null!;
    private ISoftDeleteRetentionProvider _retentionProvider = null!;
    private GetDeletedIncomeQueryHandler _sut = null!;
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
        attachCommand.CommandText = "ATTACH DATABASE ':memory:' AS income;";
        attachCommand.ExecuteNonQuery();

        using SqliteCommand createTablesCommand = _connection.CreateCommand();
        createTablesCommand.CommandText =
            """
            CREATE TABLE income.incomes (
                id TEXT PRIMARY KEY,
                user_id TEXT NOT NULL,
                amount REAL NOT NULL,
                currency TEXT NOT NULL,
                income_date TEXT NOT NULL,
                source TEXT NOT NULL,
                type TEXT NOT NULL,
                note TEXT NOT NULL,
                deleted_at_utc TEXT NULL
            );
            """;
        createTablesCommand.ExecuteNonQuery();

#pragma warning disable CA2012
        _dbConnectionFactory.OpenConnectionAsync()
            .Returns(new ValueTask<System.Data.Common.DbConnection>(_connection));
#pragma warning restore CA2012

        _sut = new GetDeletedIncomeQueryHandler(_dbConnectionFactory, _dateTimeProvider, _retentionProvider);
    }

    [TearDown]
    public void TearDown()
    {
        _connection.Dispose();
    }

    [Test]
    public async Task Handle_WhenDeletedIncomeIsPastRetention_ShouldClampDaysUntilPermanentDeletionToZero()
    {
        var userId = Guid.NewGuid();
        var incomeId = Guid.NewGuid();
        var deletedAtUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        InsertDeletedIncome(incomeId, userId, deletedAtUtc);
        _dateTimeProvider.UtcNow.Returns(new DateTime(2026, 2, 20, 0, 0, 0, DateTimeKind.Utc));

        Result<DeletedIncomePageResponse> result = await _sut.Handle(new GetDeletedIncomeQuery(userId, 1, 20), CancellationToken.None);

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

        for (int i = 0; i < 120; i++)
        {
            InsertDeletedIncome(
                Guid.NewGuid(),
                userId,
                new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc).AddMinutes(i));
        }

        _dateTimeProvider.UtcNow.Returns(new DateTime(2026, 2, 28, 0, 0, 0, DateTimeKind.Utc));

        Result<DeletedIncomePageResponse> invalidResult = await _sut.Handle(new GetDeletedIncomeQuery(userId, 0, -5), CancellationToken.None);

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

        for (int i = 0; i < 120; i++)
        {
            InsertDeletedIncome(
                Guid.NewGuid(),
                userId,
                new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc).AddMinutes(i));
        }

        _dateTimeProvider.UtcNow.Returns(new DateTime(2026, 2, 28, 0, 0, 0, DateTimeKind.Utc));

        Result<DeletedIncomePageResponse> result = await _sut.Handle(new GetDeletedIncomeQuery(userId, 1, 1000), CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Value.PageSize, Is.EqualTo(100));
            Assert.That(result.Value.TotalCount, Is.EqualTo(120));
            Assert.That(result.Value.TotalPages, Is.EqualTo(2));
            Assert.That(result.Value.Items, Has.Count.EqualTo(100));
        }
    }

    private void InsertDeletedIncome(Guid incomeId, Guid userId, DateTime deletedAtUtc)
    {
        using SqliteCommand command = _connection.CreateCommand();
        command.CommandText =
            """
            INSERT INTO income.incomes (
                id, user_id, amount, currency, income_date, source, type, note, deleted_at_utc
            )
            VALUES (
                @id, @userId, @amount, @currency, @incomeDate, @source, @type, @note, @deletedAtUtc
            );
            """;
        command.Parameters.AddWithValue("@id", incomeId.ToString());
        command.Parameters.AddWithValue("@userId", userId.ToString());
        command.Parameters.AddWithValue("@amount", 100m);
        command.Parameters.AddWithValue("@currency", "GBP");
        command.Parameters.AddWithValue("@incomeDate", "2026-02-10");
        command.Parameters.AddWithValue("@source", "ACME");
        command.Parameters.AddWithValue("@type", "Salary");
        command.Parameters.AddWithValue("@note", "Monthly salary");
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
