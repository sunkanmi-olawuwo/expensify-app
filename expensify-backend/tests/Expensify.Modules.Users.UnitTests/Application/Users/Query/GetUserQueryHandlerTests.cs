using System.Data;
using Dapper;
using Microsoft.Data.Sqlite;
using NSubstitute;
using Expensify.Common.Application.Data;
using Expensify.Common.Domain;
using Expensify.Modules.Users.Application.Abstractions;
using Expensify.Modules.Users.Application.Users.Query.GetUser;
using Expensify.Modules.Users.Domain.Users;

namespace Expensify.Modules.Users.UnitTests.Application.Users.Query;

[TestFixture]
internal sealed class GetUserQueryHandlerTests
{
    private IDbConnectionFactory _dbConnectionFactory;
    private GetUserQueryHandler _sut;
    private SqliteConnection _connection;

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

        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        using SqliteCommand attachCmd = _connection.CreateCommand();
        attachCmd.CommandText = "ATTACH DATABASE ':memory:' AS users;";
        attachCmd.ExecuteNonQuery();

        using SqliteCommand createCmd = _connection.CreateCommand();
        createCmd.CommandText =
            """
            CREATE TABLE users.users (
                id TEXT PRIMARY KEY,
                first_name TEXT NOT NULL,
                last_name TEXT NOT NULL,
                currency TEXT NOT NULL,
                timezone TEXT NOT NULL,
                month_start_day INTEGER NOT NULL
            );
            """;
        createCmd.ExecuteNonQuery();

        #pragma warning disable CA2012
                _dbConnectionFactory.OpenConnectionAsync()
                    .Returns(new ValueTask<System.Data.Common.DbConnection>(_connection));
        #pragma warning restore CA2012

        _sut = new GetUserQueryHandler(_dbConnectionFactory);
    }

    [TearDown]
    public void TearDown()
    {
        _connection?.Dispose();
    }

    [Test]
    public async Task Handle_WhenUserExists_ShouldReturnUserResponse()
    {
        // Arrange
        var userId = Guid.NewGuid();
        InsertUser(userId, "John", "Doe", "USD", "UTC", 1);
        var query = new GetUserQuery(userId);

        // Act
        Result<GetUserResponse> result = await _sut.Handle(query, CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            // Assert
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Value.Id, Is.EqualTo(userId));
            Assert.That(result.Value.FirstName, Is.EqualTo("John"));
            Assert.That(result.Value.LastName, Is.EqualTo("Doe"));
            Assert.That(result.Value.Currency, Is.EqualTo("USD"));
            Assert.That(result.Value.Timezone, Is.EqualTo("UTC"));
            Assert.That(result.Value.MonthStartDay, Is.EqualTo(1));
        }
    }

    [Test]
    public async Task Handle_WhenUserDoesNotExist_ShouldReturnFailure()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var query = new GetUserQuery(userId);

        // Act
        Result<GetUserResponse> result = await _sut.Handle(query, CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            // Assert
            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error, Is.EqualTo(UserErrors.NotFound(userId)));
        }
    }

    [Test]
    public async Task Handle_WhenUserDoesNotExist_ShouldNotReturnSuccess()
    {
        // Arrange
        var query = new GetUserQuery(Guid.NewGuid());

        // Act
        Result<GetUserResponse> result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        Assert.That(result.IsSuccess, Is.False);
    }

    [Test]
    public async Task Handle_WhenUserExists_ShouldReturnCorrectFirstName()
    {
        // Arrange
        var userId = Guid.NewGuid();
        InsertUser(userId, "Jane", "Smith", "EUR", "Europe/London", 5);
        var query = new GetUserQuery(userId);

        // Act
        Result<GetUserResponse> result = await _sut.Handle(query, CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            // Assert
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Value.FirstName, Is.EqualTo("Jane"));
            Assert.That(result.Value.LastName, Is.EqualTo("Smith"));
            Assert.That(result.Value.Currency, Is.EqualTo("EUR"));
            Assert.That(result.Value.Timezone, Is.EqualTo("Europe/London"));
            Assert.That(result.Value.MonthStartDay, Is.EqualTo(5));
        }
    }

    private void InsertUser(Guid id, string firstName, string lastName, string currency, string timezone, int monthStartDay)
    {
        using SqliteCommand cmd = _connection.CreateCommand();
        cmd.CommandText = """
            INSERT INTO users.users (id, first_name, last_name, currency, timezone, month_start_day)
            VALUES (@id, @firstName, @lastName, @currency, @timezone, @monthStartDay);
            """;
        cmd.Parameters.AddWithValue("@id", id.ToString());
        cmd.Parameters.AddWithValue("@firstName", firstName);
        cmd.Parameters.AddWithValue("@lastName", lastName);
        cmd.Parameters.AddWithValue("@currency", currency);
        cmd.Parameters.AddWithValue("@timezone", timezone);
        cmd.Parameters.AddWithValue("@monthStartDay", monthStartDay);
        cmd.ExecuteNonQuery();
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
