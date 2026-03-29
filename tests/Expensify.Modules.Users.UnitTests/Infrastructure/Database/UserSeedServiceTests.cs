using System.Security.Claims;
using Expensify.Modules.Users.Domain.Currencies;
using Expensify.Modules.Users.Domain.Identity;
using Expensify.Modules.Users.Domain.Policies;
using Expensify.Modules.Users.Domain.Timezones;
using Expensify.Modules.Users.Domain.Users;
using Expensify.Modules.Users.Infrastructure.Database;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Expensify.Modules.Users.UnitTests.Infrastructure.Database;

[TestFixture]
internal sealed class UserSeedServiceTests
{
    private SqliteConnection _connection = null!;
    private UsersDbContext _dbContext = null!;
    private UserManager<IdentityUser> _userManager = null!;
    private RoleManager<Role> _roleManager = null!;
    private UserSeedService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        DbContextOptions<UsersDbContext> options = new DbContextOptionsBuilder<UsersDbContext>()
            .UseSqlite(_connection)
            .UseSnakeCaseNamingConvention()
            .Options;

        _dbContext = new UsersDbContext(options);
        _dbContext.Database.EnsureCreated();
        _dbContext.Currencies.Add(Currency.Create("GBP", "British Pound", "GBP", 2, true, true, 0));
        _dbContext.Timezones.Add(Timezone.Create("UTC", "UTC", true, true, 0));
        _dbContext.SaveChanges();

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

    [Test]
    public async Task SeedUsersAsync_WhenUsersAlreadyExist_ShouldSkipUserCreation()
    {
        var existingUser = User.Create("Existing", "User", "identity-1", "GBP", "UTC");
        _dbContext.Users.Add(existingUser);
        await _dbContext.SaveChangesAsync();
        SetupSuccessfulRoleCreation();

        await _sut.SeedUsersAsync();

        await _userManager.DidNotReceive().CreateAsync(Arg.Any<IdentityUser>(), Arg.Any<string>());
    }

    [Test]
    public async Task SeedUsersAsync_WhenRolesDoNotExist_ShouldCreateBothRoles()
    {
        SetupSuccessfulSeeding();

        await _sut.SeedUsersAsync();

        await _roleManager.Received(1).CreateAsync(Arg.Is<Role>(role => role.Name == "Admin"));
        await _roleManager.Received(1).CreateAsync(Arg.Is<Role>(role => role.Name == "User"));
    }

    [Test]
    public async Task SeedUsersAsync_WhenRolesAlreadyExist_ShouldNotRecreate()
    {
        var adminRole = new Role { Name = "Admin" };
        var userRole = new Role { Name = "User" };
        _roleManager.FindByNameAsync("Admin").Returns(adminRole);
        _roleManager.FindByNameAsync("User").Returns(userRole);
        SetupClaimsAndUsers();

        await _sut.SeedUsersAsync();

        await _roleManager.DidNotReceive().CreateAsync(Arg.Any<Role>());
    }

    [Test]
    public void SeedUsersAsync_WhenRoleCreationFails_ShouldThrowInvalidOperationException()
    {
        _roleManager.FindByNameAsync(Arg.Any<string>()).Returns((Role?)null);
        _roleManager.CreateAsync(Arg.Any<Role>())
            .Returns(IdentityResult.Failed(new IdentityError { Code = "Error", Description = "Creation failed" }));

        Assert.ThrowsAsync<InvalidOperationException>(() => _sut.SeedUsersAsync());
    }

    [Test]
    public void SeedUsersAsync_WhenAddClaimFails_ShouldThrowInvalidOperationException()
    {
        SetupSuccessfulRoleCreation();
        _roleManager.AddClaimAsync(Arg.Any<Role>(), Arg.Any<Claim>())
            .Returns(IdentityResult.Failed(new IdentityError { Code = "Error", Description = "Claim add failed" }));

        Assert.ThrowsAsync<InvalidOperationException>(() => _sut.SeedUsersAsync());
    }

    [Test]
    public async Task SeedUsersAsync_ShouldCreateDomainUsersForSeededIdentityUsers()
    {
        SetupSuccessfulSeeding();

        await _sut.SeedUsersAsync();

        List<User> users = await _dbContext.Users.ToListAsync();

        Assert.That(users.Count, Is.EqualTo(2), "Expected two domain users to be created.");

        User adminUser = users.Single(user => user.FirstName == "Admin" && user.LastName == "User");
        User userUser = users.Single(user => user.FirstName == "User" && user.LastName == "User");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(adminUser.IdentityId, Is.Not.Null.And.Not.Empty);
            Assert.That(userUser.IdentityId, Is.Not.Null.And.Not.Empty);
            Assert.That(adminUser.Currency, Is.EqualTo("GBP"));
            Assert.That(userUser.Timezone, Is.EqualTo("UTC"));
        }
    }

    [Test]
    public async Task SeedUsersAsync_ShouldAddAllPermissionClaimsToAdminRole()
    {
        SetupSuccessfulSeeding();

        await _sut.SeedUsersAsync();

        await _roleManager.Received(1).AddClaimAsync(
            Arg.Is<Role>(role => role.Name == "Admin"),
            Arg.Is<Claim>(claim => claim.Type == UserPolicyConsts.CreatePolicy));
        await _roleManager.Received(1).AddClaimAsync(
            Arg.Is<Role>(role => role.Name == "Admin"),
            Arg.Is<Claim>(claim => claim.Type == UserPolicyConsts.DeletePolicy));
        await _roleManager.Received(1).AddClaimAsync(
            Arg.Is<Role>(role => role.Name == "Admin"),
            Arg.Is<Claim>(claim => claim.Type == UserPolicyConsts.ManagePreferenceCatalogPolicy));
        await _roleManager.Received(1).AddClaimAsync(
            Arg.Is<Role>(role => role.Name == "Admin"),
            Arg.Is<Claim>(claim => claim.Type == "admin:income:read"));
    }

    [Test]
    public async Task SeedUsersAsync_ShouldAddAllPermissionClaimsToUserRole()
    {
        SetupSuccessfulSeeding();

        await _sut.SeedUsersAsync();

        await _roleManager.Received(1).AddClaimAsync(
            Arg.Is<Role>(role => role.Name == "User"),
            Arg.Is<Claim>(claim => claim.Type == UserPolicyConsts.ReadPolicy));
        await _roleManager.Received(1).AddClaimAsync(
            Arg.Is<Role>(role => role.Name == "User"),
            Arg.Is<Claim>(claim => claim.Type == UserPolicyConsts.UpdatePolicy));
        await _roleManager.Received(1).AddClaimAsync(
            Arg.Is<Role>(role => role.Name == "User"),
            Arg.Is<Claim>(claim => claim.Type == "income:delete"));
    }

    [Test]
    public async Task SeedUsersAsync_WhenClaimsAlreadyExist_ShouldNotDuplicate()
    {
        var adminRole = new Role { Name = "Admin" };
        var userRole = new Role { Name = "User" };
        _roleManager.FindByNameAsync("Admin").Returns(adminRole);
        _roleManager.FindByNameAsync("User").Returns(userRole);

        _roleManager.GetClaimsAsync(adminRole).Returns(
        [
            new(UserPolicyConsts.CreatePolicy, "true"),
            new(UserPolicyConsts.DeletePolicy, "true"),
            new(UserPolicyConsts.ReadPolicy, "true"),
            new(UserPolicyConsts.UpdatePolicy, "true"),
            new(UserPolicyConsts.ReadAllPolicy, "true"),
            new(UserPolicyConsts.ManagePreferenceCatalogPolicy, "true"),
            new("expenses:read", "true"),
            new("expenses:write", "true"),
            new("expenses:delete", "true"),
            new("admin:expenses:read", "true"),
            new("income:read", "true"),
            new("income:write", "true"),
            new("income:delete", "true"),
            new("admin:income:read", "true"),
            new("investments:read", "true"),
            new("investments:write", "true"),
            new("investments:delete", "true"),
            new("admin:investments:read", "true"),
            new("admin:investments:manage-categories", "true"),
            new("dashboard:read", "true"),
        ]);
        _roleManager.GetClaimsAsync(userRole).Returns(
        [
            new(UserPolicyConsts.ReadPolicy, "true"),
            new(UserPolicyConsts.UpdatePolicy, "true"),
            new("expenses:read", "true"),
            new("expenses:write", "true"),
            new("expenses:delete", "true"),
            new("income:read", "true"),
            new("income:write", "true"),
            new("income:delete", "true"),
            new("investments:read", "true"),
            new("investments:write", "true"),
            new("investments:delete", "true"),
            new("dashboard:read", "true"),
        ]);

        SetupUserCreation();

        await _sut.SeedUsersAsync();

        await _roleManager.DidNotReceive().AddClaimAsync(Arg.Any<Role>(), Arg.Any<Claim>());
    }

    [Test]
    public async Task SeedUsersAsync_WhenOnlySomeClaimsExist_ShouldAddOnlyMissingClaims()
    {
        var adminRole = new Role { Name = "Admin" };
        var userRole = new Role { Name = "User" };
        _roleManager.FindByNameAsync("Admin").Returns(adminRole);
        _roleManager.FindByNameAsync("User").Returns(userRole);
        _roleManager.GetClaimsAsync(adminRole).Returns(
        [
            new(UserPolicyConsts.CreatePolicy, "true"),
            new("expenses:read", "true"),
            new("investments:read", "true"),
        ]);
        _roleManager.GetClaimsAsync(userRole).Returns(
        [
            new(UserPolicyConsts.ReadPolicy, "true"),
            new("income:read", "true"),
            new("investments:read", "true"),
        ]);
        _roleManager.AddClaimAsync(Arg.Any<Role>(), Arg.Any<Claim>()).Returns(IdentityResult.Success);
        SetupUserCreation();

        await _sut.SeedUsersAsync();

        await _roleManager.DidNotReceive().AddClaimAsync(
            Arg.Is<Role>(role => role.Name == "Admin"),
            Arg.Is<Claim>(claim => claim.Type == UserPolicyConsts.CreatePolicy));
        await _roleManager.Received(1).AddClaimAsync(
            Arg.Is<Role>(role => role.Name == "Admin"),
            Arg.Is<Claim>(claim => claim.Type == UserPolicyConsts.DeletePolicy));
        await _roleManager.DidNotReceive().AddClaimAsync(
            Arg.Is<Role>(role => role.Name == "User"),
            Arg.Is<Claim>(claim => claim.Type == UserPolicyConsts.ReadPolicy));
        await _roleManager.Received(1).AddClaimAsync(
            Arg.Is<Role>(role => role.Name == "User"),
            Arg.Is<Claim>(claim => claim.Type == UserPolicyConsts.UpdatePolicy));
    }

    [Test]
    public async Task SeedUsersAsync_ShouldCreateAdminAndUserIdentityUsers()
    {
        SetupSuccessfulSeeding();

        await _sut.SeedUsersAsync();

        await _userManager.Received(1).CreateAsync(
            Arg.Is<IdentityUser>(user => user.Email == "admin@test.com"), "Test1234!");
        await _userManager.Received(1).CreateAsync(
            Arg.Is<IdentityUser>(user => user.Email == "user@test.com"), "Test1234!");
    }

    [Test]
    public async Task SeedUsersAsync_ShouldAssignCorrectRolesToUsers()
    {
        SetupSuccessfulSeeding();

        await _sut.SeedUsersAsync();

        await _userManager.Received(1).AddToRoleAsync(
            Arg.Is<IdentityUser>(user => user.Email == "admin@test.com"), "Admin");
        await _userManager.Received(1).AddToRoleAsync(
            Arg.Is<IdentityUser>(user => user.Email == "user@test.com"), "User");
    }

    [Test]
    public async Task SeedUsersAsync_WhenIdentityUsersAlreadyExist_ShouldSkipCreation()
    {
        SetupSuccessfulRoleCreation();

        _userManager.FindByEmailAsync("admin@test.com")
            .Returns(new IdentityUser { Id = "id-1", Email = "admin@test.com" });
        _userManager.FindByEmailAsync("user@test.com")
            .Returns(new IdentityUser { Id = "id-2", Email = "user@test.com" });

        await _sut.SeedUsersAsync();

        await _userManager.DidNotReceive().CreateAsync(Arg.Any<IdentityUser>(), Arg.Any<string>());
    }

    [Test]
    public void SeedUsersAsync_WhenUserCreationFails_ShouldThrowInvalidOperationException()
    {
        SetupSuccessfulRoleCreation();

        _userManager.FindByEmailAsync(Arg.Any<string>()).Returns((IdentityUser?)null);
        _userManager.CreateAsync(Arg.Any<IdentityUser>(), Arg.Any<string>())
            .Returns(IdentityResult.Failed(new IdentityError { Code = "Error", Description = "Creation failed" }));

        Assert.ThrowsAsync<InvalidOperationException>(() => _sut.SeedUsersAsync());
    }

    [Test]
    public void SeedUsersAsync_WhenRoleAssignmentFails_ShouldThrowInvalidOperationException()
    {
        SetupSuccessfulRoleCreation();

        _userManager.FindByEmailAsync(Arg.Any<string>()).Returns((IdentityUser?)null);
        _userManager.CreateAsync(Arg.Any<IdentityUser>(), Arg.Any<string>()).Returns(IdentityResult.Success);
        _userManager.AddToRoleAsync(Arg.Any<IdentityUser>(), Arg.Any<string>())
            .Returns(IdentityResult.Failed(new IdentityError { Code = "Error", Description = "Assignment failed" }));

        Assert.ThrowsAsync<InvalidOperationException>(() => _sut.SeedUsersAsync());
    }

    [Test]
    public void SeedUsersAsync_WhenDefaultCurrencyMissing_ShouldThrowInvalidOperationException()
    {
        SetupSuccessfulRoleCreation();
        SetupUserCreation();
        _dbContext.Currencies.RemoveRange(_dbContext.Currencies);
        _dbContext.SaveChanges();

        Assert.ThrowsAsync<InvalidOperationException>(() => _sut.SeedUsersAsync());
    }

    [Test]
    public void SeedUsersAsync_WhenDefaultTimezoneMissing_ShouldThrowInvalidOperationException()
    {
        SetupSuccessfulRoleCreation();
        SetupUserCreation();
        _dbContext.Timezones.RemoveRange(_dbContext.Timezones);
        _dbContext.SaveChanges();

        Assert.ThrowsAsync<InvalidOperationException>(() => _sut.SeedUsersAsync());
    }

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
}
