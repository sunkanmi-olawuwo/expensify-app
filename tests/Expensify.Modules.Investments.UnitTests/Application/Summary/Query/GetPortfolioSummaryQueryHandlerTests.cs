using System.Data;
using Dapper;
using Microsoft.Data.Sqlite;
using NSubstitute;
using Expensify.Common.Application.Data;
using Expensify.Common.Domain;
using Expensify.Modules.Investments.Application.Abstractions;
using Expensify.Modules.Investments.Application.Abstractions.Users;
using Expensify.Modules.Investments.Application.Summary.Query.GetPortfolioSummary;

namespace Expensify.Modules.Investments.UnitTests.Application.Summary.Query;

[TestFixture]
internal sealed class GetPortfolioSummaryQueryHandlerTests
{
    private IDbConnectionFactory _dbConnectionFactory = null!;
    private IUserSettingsService _userSettingsService = null!;
    private GetPortfolioSummaryQueryHandler _sut = null!;
    private SqliteConnection _connection = null!;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        SqlMapper.RemoveTypeMap(typeof(Guid));
        SqlMapper.AddTypeHandler(new GuidTypeHandler());
    }

    [SetUp]
    public void SetUp()
    {
        _dbConnectionFactory = Substitute.For<IDbConnectionFactory>();
        _userSettingsService = Substitute.For<IUserSettingsService>();

        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        using SqliteCommand attachCommand = _connection.CreateCommand();
        attachCommand.CommandText = "ATTACH DATABASE ':memory:' AS investments;";
        attachCommand.ExecuteNonQuery();

        using SqliteCommand createTablesCommand = _connection.CreateCommand();
        createTablesCommand.CommandText =
            """
            CREATE TABLE investments.investment_accounts (
                id TEXT PRIMARY KEY,
                user_id TEXT NOT NULL,
                current_balance REAL NOT NULL,
                deleted_at_utc TEXT NULL
            );

            CREATE TABLE investments.investment_contributions (
                id TEXT PRIMARY KEY,
                investment_id TEXT NOT NULL,
                amount REAL NOT NULL,
                deleted_at_utc TEXT NULL
            );
            """;
        createTablesCommand.ExecuteNonQuery();

#pragma warning disable CA2012
        _dbConnectionFactory.OpenConnectionAsync()
            .Returns(new ValueTask<System.Data.Common.DbConnection>(_connection));
#pragma warning restore CA2012

        _sut = new GetPortfolioSummaryQueryHandler(_dbConnectionFactory, _userSettingsService);
    }

    [TearDown]
    public void TearDown()
    {
        _connection.Dispose();
    }

    [Test]
    public async Task Handle_WhenPortfolioIsEmpty_ShouldReturnZeroSummary()
    {
        var userId = Guid.NewGuid();
        _userSettingsService.GetSettingsAsync(userId, Arg.Any<CancellationToken>())
            .Returns(Result.Success(new UserSettingsResponse("GBP", "UTC", 1)));

        Result<PortfolioSummaryResponse> result = await _sut.Handle(new GetPortfolioSummaryQuery(userId), CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Value.TotalContributed, Is.EqualTo(0m));
            Assert.That(result.Value.CurrentValue, Is.EqualTo(0m));
            Assert.That(result.Value.TotalGainLoss, Is.EqualTo(0m));
            Assert.That(result.Value.GainLossPercentage, Is.EqualTo(0m));
            Assert.That(result.Value.Currency, Is.EqualTo("GBP"));
        }
    }

    [Test]
    public async Task Handle_WhenPortfolioHasData_ShouldCalculateGainLossAndPercentage()
    {
        var userId = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        var otherAccountId = Guid.NewGuid();

        _userSettingsService.GetSettingsAsync(userId, Arg.Any<CancellationToken>())
            .Returns(Result.Success(new UserSettingsResponse("GBP", "UTC", 1)));

        InsertAccount(accountId, userId, 1200m);
        InsertAccount(otherAccountId, Guid.NewGuid(), 999m);
        InsertContribution(Guid.NewGuid(), accountId, 400m);
        InsertContribution(Guid.NewGuid(), accountId, 500m);
        InsertContribution(Guid.NewGuid(), otherAccountId, 999m);

        Result<PortfolioSummaryResponse> result = await _sut.Handle(new GetPortfolioSummaryQuery(userId), CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Value.TotalContributed, Is.EqualTo(900m));
            Assert.That(result.Value.CurrentValue, Is.EqualTo(1200m));
            Assert.That(result.Value.TotalGainLoss, Is.EqualTo(300m));
            Assert.That(result.Value.GainLossPercentage, Is.EqualTo(33.333333333333336m).Within(0.0001m));
            Assert.That(result.Value.AccountCount, Is.EqualTo(1));
        }
    }

    private void InsertAccount(Guid id, Guid userId, decimal currentBalance, string? deletedAtUtc = null)
    {
        using SqliteCommand command = _connection.CreateCommand();
        command.CommandText =
            """
            INSERT INTO investments.investment_accounts (id, user_id, current_balance, deleted_at_utc)
            VALUES (@id, @userId, @currentBalance, @deletedAtUtc);
            """;
        command.Parameters.AddWithValue("@id", id.ToString());
        command.Parameters.AddWithValue("@userId", userId.ToString());
        command.Parameters.AddWithValue("@currentBalance", currentBalance);
        command.Parameters.AddWithValue("@deletedAtUtc", (object?)deletedAtUtc ?? DBNull.Value);
        command.ExecuteNonQuery();
    }

    private void InsertContribution(Guid id, Guid investmentId, decimal amount, string? deletedAtUtc = null)
    {
        using SqliteCommand command = _connection.CreateCommand();
        command.CommandText =
            """
            INSERT INTO investments.investment_contributions (id, investment_id, amount, deleted_at_utc)
            VALUES (@id, @investmentId, @amount, @deletedAtUtc);
            """;
        command.Parameters.AddWithValue("@id", id.ToString());
        command.Parameters.AddWithValue("@investmentId", investmentId.ToString());
        command.Parameters.AddWithValue("@amount", amount);
        command.Parameters.AddWithValue("@deletedAtUtc", (object?)deletedAtUtc ?? DBNull.Value);
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
}
