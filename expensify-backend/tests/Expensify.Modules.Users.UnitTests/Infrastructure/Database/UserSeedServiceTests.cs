using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Expensify.Modules.Users.Domain.Identity;
using Expensify.Modules.Users.Domain.Policies;
using Expensify.Modules.Users.Domain.Users;
using Expensify.Modules.Users.Infrastructure.Database;

namespace Expensify.Modules.Users.UnitTests.Infrastructure.Database;

[TestFixture]
internal sealed class UserSeedServiceTests
{
    private SqliteConnection _connection;
    private UsersDbContext _dbContext;
    private UserManager<IdentityUser> _userManager;
    private RoleManager<Role> _roleManager;
    private UserSeedService _sut;

    [SetUp]
    public void SetUp()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        DbContextOptions<UsersDbContext> options = new DbContextOptionsBuilder<UsersDbContext>()
            .UseSqlite(_connection)
            .Options;

        _dbContext = new UsersDbContext(options);
        _dbContext.Database.EnsureCreated();

        IUserStore<IdentityUser> userStore = Substitute.For<IUserStore<IdentityUser>>();
        _userManager = Substitute.For<UserManager<IdentityUser>>(
            userStore, null, null, null, null, null, null, null, null);

        IRoleStore<Role> roleStore = Substitute.For<IRoleStore<Role>>();
        _roleManager = Substitute.For<RoleManager<Role>>(
            roleStore, null, null, null, null);

        _sut = new UserSeedService(
            _dbContext,
            _userManager,
            _roleManager,
            NullLogger<UserSeedService>.Instance);
    }

    [TearDown]
    public void TearDown()
    {
        _dbContext?.Dispose();
        _connection?.Dispose();
        _userManager?.Dispose();
        _roleManager?.Dispose();
    }

    #region SeedUsersAsync – Early Exit

    [Test]
    public async Task SeedUsersAsync_WhenUsersAlreadyExist_ShouldSkipSeeding()
    {
        // Arrange
        var existingUser = User.Create("Existing", "User", "identity-1");
        _dbContext.Users.Add(existingUser);
        await _dbContext.SaveChangesAsync();

        // Act
        await _sut.SeedUsersAsync();

        // Assert
        await _roleManager.DidNotReceive().CreateAsync(Arg.Any<Role>());
        await _userManager.DidNotReceive().CreateAsync(Arg.Any<IdentityUser>(), Arg.Any<string>());
    }

    #endregion

    #region SeedUsersAsync – Role Creation

    [Test]
    public async Task SeedUsersAsync_WhenRolesDoNotExist_ShouldCreateBothRoles()
    {
        // Arrange
        SetupSuccessfulSeeding();

        // Act
        await _sut.SeedUsersAsync();

        // Assert
        await _roleManager.Received(1).CreateAsync(Arg.Is<Role>(r => r.Name == "Admin"));
        await _roleManager.Received(1).CreateAsync(Arg.Is<Role>(r => r.Name == "Tutor"));
    }

    [Test]
    public async Task SeedUsersAsync_WhenRolesAlreadyExist_ShouldNotRecreate()
    {
        // Arrange
        var adminRole = new Role { Name = "Admin" };
        var tutorRole = new Role { Name = "Tutor" };
        _roleManager.FindByNameAsync("Admin").Returns(adminRole);
        _roleManager.FindByNameAsync("Tutor").Returns(tutorRole);
        SetupClaimsAndUsers();

        // Act
        await _sut.SeedUsersAsync();

        // Assert
        await _roleManager.DidNotReceive().CreateAsync(Arg.Any<Role>());
    }

    [Test]
    public void SeedUsersAsync_WhenRoleCreationFails_ShouldThrowInvalidOperationException()
    {
        // Arrange
        _roleManager.FindByNameAsync(Arg.Any<string>()).Returns((Role?)null);
        _roleManager.CreateAsync(Arg.Any<Role>())
            .Returns(IdentityResult.Failed(new IdentityError { Code = "Error", Description = "Creation failed" }));

        // Act & Assert
        Assert.ThrowsAsync<InvalidOperationException>(() => _sut.SeedUsersAsync());
    }

    [Test]
    public async Task SeedUsersAsync_ShouldCreateDomainUsersForSeededIdentityUsers()
    {
        // Arrange
        SetupSuccessfulSeeding();

        // Act
        await _sut.SeedUsersAsync();

        // Assert
        List<User> users = await _dbContext.Users.ToListAsync();

        Assert.That(users.Count, Is.EqualTo(2), "Expected two domain users to be created.");

        User adminUser = users.Single(u => u.FirstName == "Admin" && u.LastName == "User");
        User tutorUser = users.Single(u => u.FirstName == "Tutor" && u.LastName == "User");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(adminUser.IdentityId, Is.Not.Null.And.Not.Empty);
            Assert.That(tutorUser.IdentityId, Is.Not.Null.And.Not.Empty);
        }
    }

    #endregion

    #region SeedUsersAsync – Role Claims

    [Test]
    public async Task SeedUsersAsync_ShouldAddAllPermissionClaimsToAdminRole()
    {
        // Arrange
        SetupSuccessfulSeeding();

        // Act
        await _sut.SeedUsersAsync();

        // Assert

        await _roleManager.Received(1).AddClaimAsync(
            Arg.Is<Role>(r => r.Name == "Admin"),
            Arg.Is<Claim>(c => c.Type == UserPolicyConsts.CreatePolicy));

        await _roleManager.Received(1).AddClaimAsync(
            Arg.Is<Role>(r => r.Name == "Admin"),
            Arg.Is<Claim>(c => c.Type == UserPolicyConsts.DeletePolicy));

    }

    [Test]
    public async Task SeedUsersAsync_ShouldAddAllPermissionClaimsToTutorRole()
    {
        // Arrange
        SetupSuccessfulSeeding();

        // Act
        await _sut.SeedUsersAsync();

        // Assert
        await _roleManager.Received(1).AddClaimAsync(
            Arg.Is<Role>(r => r.Name == "Tutor"),
            Arg.Is<Claim>(c => c.Type == UserPolicyConsts.ReadPolicy));
        await _roleManager.Received(1).AddClaimAsync(
            Arg.Is<Role>(r => r.Name == "Tutor"),
            Arg.Is<Claim>(c => c.Type == UserPolicyConsts.UpdatePolicy));
    }

    [Test]
    public async Task SeedUsersAsync_WhenClaimsAlreadyExist_ShouldNotDuplicate()
    {
        // Arrange
        var adminRole = new Role { Name = "Admin" };
        var tutorRole = new Role { Name = "Tutor" };
        _roleManager.FindByNameAsync("Admin").Returns(adminRole);
        _roleManager.FindByNameAsync("Tutor").Returns(tutorRole);

        _roleManager.GetClaimsAsync(adminRole).Returns(
        [
            new(UserPolicyConsts.CreatePolicy, "true"),
            new(UserPolicyConsts.DeletePolicy, "true"),
            new(UserPolicyConsts.ReadPolicy, "true"),
            new(UserPolicyConsts.UpdatePolicy, "true"),
            new(UserPolicyConsts.ReadAllPolicy, "true"),
        ]);
        _roleManager.GetClaimsAsync(tutorRole).Returns(
        [
            new(UserPolicyConsts.ReadPolicy, "true"),
            new(UserPolicyConsts.UpdatePolicy, "true"),
        ]);

        SetupUserCreation();

        // Act
        await _sut.SeedUsersAsync();

        // Assert
        await _roleManager.DidNotReceive().AddClaimAsync(Arg.Any<Role>(), Arg.Any<Claim>());
    }

    #endregion

    #region SeedUsersAsync – User Creation

    [Test]
    public async Task SeedUsersAsync_ShouldCreateAdminAndTutorIdentityUsers()
    {
        // Arrange
        SetupSuccessfulSeeding();

        // Act
        await _sut.SeedUsersAsync();

        // Assert
        await _userManager.Received(1).CreateAsync(
            Arg.Is<IdentityUser>(u => u.Email == "admin@test.com"), "Test1234!");
        await _userManager.Received(1).CreateAsync(
            Arg.Is<IdentityUser>(u => u.Email == "tutor@test.com"), "Test1234!");
    }

    [Test]
    public async Task SeedUsersAsync_ShouldAssignCorrectRolesToUsers()
    {
        // Arrange
        SetupSuccessfulSeeding();

        // Act
        await _sut.SeedUsersAsync();

        // Assert
        await _userManager.Received(1).AddToRoleAsync(
            Arg.Is<IdentityUser>(u => u.Email == "admin@test.com"), "Admin");
        await _userManager.Received(1).AddToRoleAsync(
            Arg.Is<IdentityUser>(u => u.Email == "tutor@test.com"), "Tutor");
    }

    [Test]
    public async Task SeedUsersAsync_WhenIdentityUsersAlreadyExist_ShouldSkipCreation()
    {
        // Arrange
        SetupSuccessfulRoleCreation();

        _userManager.FindByEmailAsync("admin@test.com")
            .Returns(new IdentityUser { Id = "id-1", Email = "admin@test.com" });
        _userManager.FindByEmailAsync("tutor@test.com")
            .Returns(new IdentityUser { Id = "id-2", Email = "tutor@test.com" });

        // Act
        await _sut.SeedUsersAsync();

        // Assert
        await _userManager.DidNotReceive().CreateAsync(Arg.Any<IdentityUser>(), Arg.Any<string>());
    }

    [Test]
    public void SeedUsersAsync_WhenUserCreationFails_ShouldThrowInvalidOperationException()
    {
        // Arrange
        SetupSuccessfulRoleCreation();

        _userManager.FindByEmailAsync(Arg.Any<string>()).Returns((IdentityUser?)null);
        _userManager.CreateAsync(Arg.Any<IdentityUser>(), Arg.Any<string>())
            .Returns(IdentityResult.Failed(new IdentityError { Code = "Error", Description = "Creation failed" }));

        // Act & Assert
        Assert.ThrowsAsync<InvalidOperationException>(() => _sut.SeedUsersAsync());
    }

    [Test]
    public void SeedUsersAsync_WhenRoleAssignmentFails_ShouldThrowInvalidOperationException()
    {
        // Arrange
        SetupSuccessfulRoleCreation();

        _userManager.FindByEmailAsync(Arg.Any<string>()).Returns((IdentityUser?)null);
        _userManager.CreateAsync(Arg.Any<IdentityUser>(), Arg.Any<string>())
            .Returns(IdentityResult.Success);
        _userManager.AddToRoleAsync(Arg.Any<IdentityUser>(), Arg.Any<string>())
            .Returns(IdentityResult.Failed(new IdentityError { Code = "Error", Description = "Assignment failed" }));

        // Act & Assert
        Assert.ThrowsAsync<InvalidOperationException>(() => _sut.SeedUsersAsync());
    }

    #endregion

    #region Helpers

    private void SetupSuccessfulSeeding()
    {
        SetupSuccessfulRoleCreation();
        SetupUserCreation();
    }

    private void SetupSuccessfulRoleCreation()
    {
        _roleManager.FindByNameAsync(Arg.Any<string>()).Returns((Role?)null);
        _roleManager.CreateAsync(Arg.Any<Role>()).Returns(IdentityResult.Success);
        _roleManager.GetClaimsAsync(Arg.Any<Role>()).Returns([]);
        _roleManager.AddClaimAsync(Arg.Any<Role>(), Arg.Any<Claim>()).Returns(IdentityResult.Success);
    }

    private void SetupUserCreation()
    {
        _userManager.FindByEmailAsync(Arg.Any<string>()).Returns((IdentityUser?)null);
        _userManager.CreateAsync(Arg.Any<IdentityUser>(), Arg.Any<string>()).Returns(IdentityResult.Success);
        _userManager.AddToRoleAsync(Arg.Any<IdentityUser>(), Arg.Any<string>()).Returns(IdentityResult.Success);
    }

    private void SetupClaimsAndUsers()
    {
        _roleManager.GetClaimsAsync(Arg.Any<Role>()).Returns([]);
        _roleManager.AddClaimAsync(Arg.Any<Role>(), Arg.Any<Claim>()).Returns(IdentityResult.Success);
        SetupUserCreation();
    }

    #endregion
}
