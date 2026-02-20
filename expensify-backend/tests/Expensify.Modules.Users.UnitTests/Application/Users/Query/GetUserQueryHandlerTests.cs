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
                last_name TEXT NOT NULL
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
        InsertUser(userId, "John", "Doe");
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
        InsertUser(userId, "Jane", "Smith");
        var query = new GetUserQuery(userId);

        // Act
        Result<GetUserResponse> result = await _sut.Handle(query, CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            // Assert
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Value.FirstName, Is.EqualTo("Jane"));
            Assert.That(result.Value.LastName, Is.EqualTo("Smith"));
        }
    }

    private void InsertUser(Guid id, string firstName, string lastName)
    {
        using SqliteCommand cmd = _connection.CreateCommand();
        cmd.CommandText = "INSERT INTO users.users (id, first_name, last_name) VALUES (@id, @firstName, @lastName);";
        cmd.Parameters.AddWithValue("@id", id.ToString());
        cmd.Parameters.AddWithValue("@firstName", firstName);
        cmd.Parameters.AddWithValue("@lastName", lastName);
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
