using System.Data;
using Dapper;
using Microsoft.Data.Sqlite;
using NSubstitute;
using Expensify.Common.Application.Data;
using Expensify.Common.Domain;
using Expensify.Modules.Users.Application.Abstractions;
using Expensify.Modules.Users.Application.Admin.Query.GetUsers;

namespace Expensify.Modules.Users.UnitTests.Application.Admin.Query;

[TestFixture]
internal sealed class GetUsersQueryHandlerTests
{
    private IDbConnectionFactory _dbConnectionFactory;
    private GetUsersQueryHandler _sut;
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
                identity_id TEXT NOT NULL
            );

            CREATE TABLE users.identity_users (
                id TEXT PRIMARY KEY,
                email TEXT NOT NULL
            );

            CREATE TABLE users.roles (
                id TEXT PRIMARY KEY,
                name TEXT NOT NULL
            );

            CREATE TABLE users.user_roles (
                user_id TEXT NOT NULL,
                role_id TEXT NOT NULL,
                PRIMARY KEY (user_id, role_id)
            );
            """;
        createCmd.ExecuteNonQuery();

#pragma warning disable CA2012
        _dbConnectionFactory.OpenConnectionAsync()
            .Returns(new ValueTask<System.Data.Common.DbConnection>(_connection));
#pragma warning restore CA2012

        _sut = new GetUsersQueryHandler(_dbConnectionFactory);
    }

    [TearDown]
    public void TearDown()
    {
        _connection.Dispose();
    }

    [Test]
    public async Task Handle_ShouldReturnPagedUsersAndPaginationMetadata()
    {
        // Arrange
        InsertUserWithRole(Guid.NewGuid(), "alice@example.com", "Alice", "Andrews", "Admin");
        InsertUserWithRole(Guid.NewGuid(), "bob@example.com", "Bob", "Baker", "Tutor");
        InsertUserWithRole(Guid.NewGuid(), "carol@example.com", "Carol", "Clark", "Tutor");

        GetUsersQuery query = new("", "", "Email", "", 1, 2, "asc");

        // Act
        Result<GetUsersResponse> result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Value.Page, Is.EqualTo(1));
            Assert.That(result.Value.CurentPage, Is.EqualTo(1));
            Assert.That(result.Value.PageSize, Is.EqualTo(2));
            Assert.That(result.Value.TotalCount, Is.EqualTo(3));
            Assert.That(result.Value.TotalPages, Is.EqualTo(2));
            Assert.That(result.Value.Users, Has.Count.EqualTo(2));
        }
    }

    [Test]
    public async Task Handle_ShouldFilterByRole_WhenFilterByRoleIsProvided()
    {
        // Arrange
        InsertUserWithRole(Guid.NewGuid(), "admin.user@example.com", "Admin", "One", "Admin");
        InsertUserWithRole(Guid.NewGuid(), "tutor.user@example.com", "Tutor", "One", "Tutor");

        GetUsersQuery query = new("role", "Tutor", "Email", "", 1, 10, "asc");

        // Act
        Result<GetUsersResponse> result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Value.TotalCount, Is.EqualTo(1));
            Assert.That(result.Value.Users, Has.Count.EqualTo(1));
            Assert.That(result.Value.Users.Single().Role, Is.EqualTo("Tutor"));
        }
    }

    [Test]
    public async Task Handle_ShouldApplyCaseInsensitiveSearch_OnEmailFirstNameAndLastName()
    {
        // Arrange
        InsertUserWithRole(Guid.NewGuid(), "alpha@example.com", "Jane", "Doe", "Tutor");
        InsertUserWithRole(Guid.NewGuid(), "beta@example.com", "John", "Smith", "Tutor");

        GetUsersQuery query = new("", "", "Email", "jAnE", 1, 10, "asc");

        // Act
        Result<GetUsersResponse> result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Value.TotalCount, Is.EqualTo(1));
            Assert.That(result.Value.Users, Has.Count.EqualTo(1));
            Assert.That(result.Value.Users.Single().FirstName, Is.EqualTo("Jane"));
        }
    }

    [Test]
    public async Task Handle_ShouldFallbackToDefaultSortByEmail_WhenSortByIsInvalid()
    {
        // Arrange
        InsertUserWithRole(Guid.NewGuid(), "zeta@example.com", "Zeta", "User", "Tutor");
        InsertUserWithRole(Guid.NewGuid(), "alpha@example.com", "Alpha", "User", "Tutor");

        GetUsersQuery query = new("", "", "invalid-sort", "", 1, 10, "asc");

        // Act
        Result<GetUsersResponse> result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        var users = result.Value.Users.ToList();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(users, Has.Count.EqualTo(2));
            Assert.That(users[0].Email, Is.EqualTo("alpha@example.com"));
            Assert.That(users[1].Email, Is.EqualTo("zeta@example.com"));
        }
    }

    [Test]
    public async Task Handle_ShouldNormalizePageAndPageSize_WhenInvalidValuesAreProvided()
    {
        // Arrange
        for (int i = 0; i < 110; i++)
        {
            InsertUserWithRole(Guid.NewGuid(), $"user{i:D3}@example.com", $"First{i}", $"Last{i}", "Tutor");
        }

        GetUsersQuery query = new("", "", "Email", "", 0, -3, "asc");

        // Act
        Result<GetUsersResponse> result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Value.Page, Is.EqualTo(1));
            Assert.That(result.Value.PageSize, Is.EqualTo(10));
            Assert.That(result.Value.TotalCount, Is.EqualTo(110));
            Assert.That(result.Value.Users, Has.Count.EqualTo(10));
        }
    }

    private void InsertUserWithRole(Guid userId, string email, string firstName, string lastName, string roleName)
    {
        string identityId = Guid.NewGuid().ToString();
        string roleId = GetOrCreateRole(roleName);

        using SqliteCommand identityCommand = _connection.CreateCommand();
        identityCommand.CommandText =
            "INSERT INTO users.identity_users (id, email) VALUES (@id, @email);";
        identityCommand.Parameters.AddWithValue("@id", identityId);
        identityCommand.Parameters.AddWithValue("@email", email);
        identityCommand.ExecuteNonQuery();

        using SqliteCommand userCommand = _connection.CreateCommand();
        userCommand.CommandText =
            "INSERT INTO users.users (id, first_name, last_name, identity_id) VALUES (@id, @firstName, @lastName, @identityId);";
        userCommand.Parameters.AddWithValue("@id", userId.ToString());
        userCommand.Parameters.AddWithValue("@firstName", firstName);
        userCommand.Parameters.AddWithValue("@lastName", lastName);
        userCommand.Parameters.AddWithValue("@identityId", identityId);
        userCommand.ExecuteNonQuery();

        using SqliteCommand userRoleCommand = _connection.CreateCommand();
        userRoleCommand.CommandText =
            "INSERT INTO users.user_roles (user_id, role_id) VALUES (@userId, @roleId);";
        userRoleCommand.Parameters.AddWithValue("@userId", identityId);
        userRoleCommand.Parameters.AddWithValue("@roleId", roleId);
        userRoleCommand.ExecuteNonQuery();
    }

    private string GetOrCreateRole(string roleName)
    {
        using SqliteCommand selectCommand = _connection.CreateCommand();
        selectCommand.CommandText = "SELECT id FROM users.roles WHERE name = @name LIMIT 1;";
        selectCommand.Parameters.AddWithValue("@name", roleName);
        object? existing = selectCommand.ExecuteScalar();

        if (existing is string id)
        {
            return id;
        }

        string roleId = Guid.NewGuid().ToString();

        using SqliteCommand insertCommand = _connection.CreateCommand();
        insertCommand.CommandText = "INSERT INTO users.roles (id, name) VALUES (@id, @name);";
        insertCommand.Parameters.AddWithValue("@id", roleId);
        insertCommand.Parameters.AddWithValue("@name", roleName);
        insertCommand.ExecuteNonQuery();

        return roleId;
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
