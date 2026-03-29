using Bogus;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Expensify.Modules.Users.Domain.Currencies;
using Expensify.Modules.Users.Domain.Policies;
using Expensify.Modules.Users.Domain.Timezones;
using Expensify.Modules.Users.Domain.Users;
using Expensify.Modules.Users.Domain.Identity;

namespace Expensify.Modules.Users.Infrastructure.Database;

public class UserSeedService
(UsersDbContext dbContext,
    UserManager<IdentityUser> userManager,
    RoleManager<Role> roleManager,
    ILogger<UserSeedService> logger
 )
{
    private const string ExpenseReadPolicy = "expenses:read";
    private const string ExpenseWritePolicy = "expenses:write";
    private const string ExpenseDeletePolicy = "expenses:delete";
    private const string ExpenseAdminReadPolicy = "admin:expenses:read";
    private const string IncomeReadPolicy = "income:read";
    private const string IncomeWritePolicy = "income:write";
    private const string IncomeDeletePolicy = "income:delete";
    private const string IncomeAdminReadPolicy = "admin:income:read";
    private const string InvestmentsReadPolicy = "investments:read";
    private const string InvestmentsWritePolicy = "investments:write";
    private const string InvestmentsDeletePolicy = "investments:delete";
    private const string InvestmentsAdminReadPolicy = "admin:investments:read";
    private const string InvestmentsAdminManageCategoriesPolicy = "admin:investments:manage-categories";
    private const string DashboardReadPolicy = "dashboard:read";

    public async Task SeedUsersAsync()
    {
        SetRandomizerSeed();

        logger.LogInformation("Starting user seeding...");

        Role adminRole = await EnsureRoleAsync(AdminRoleType.Admin.ToString());
        Role userRole = await EnsureRoleAsync(RoleType.User.ToString());

        await ConfigureAdminRolePermissions(adminRole);
        await ConfigureUserRolePermissions(userRole);

        if (await dbContext.Users.AnyAsync())
        {
            logger.LogInformation("Users already exist, skipping user creation");
            return;
        }

        await CreateUserAsync("admin@test.com", "Test1234!", AdminRoleType.Admin.ToString());
        await CreateUserAsync("user@test.com", "Test1234!", RoleType.User.ToString());

        await dbContext.SaveChangesAsync();

        logger.LogInformation("User seeding completed");
    }

    private static void SetRandomizerSeed()
    {
        Randomizer.Seed = new Random(4503);
    }

    private async Task<Role> EnsureRoleAsync(string roleName)
    {
        Role? existingRole = await roleManager.FindByNameAsync(roleName);
        if (existingRole is not null)
        {
            logger.LogInformation("Role {RoleName} already exists, skipping creation", roleName);
            return existingRole;
        }

        var newRole = new Role { Name = roleName };
        IdentityResult result = await roleManager.CreateAsync(newRole);

        if (!result.Succeeded)
        {
            string errors = string.Join(", ", result.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Failed to create role '{roleName}': {errors}");
        }

        return newRole;
    }

    private async Task ConfigureAdminRolePermissions(Role adminRole)
    {
        IList<Claim> existingClaims = await roleManager.GetClaimsAsync(adminRole);

        await AddClaimIfMissingAsync(adminRole, existingClaims, UserPolicyConsts.CreatePolicy);
        await AddClaimIfMissingAsync(adminRole, existingClaims, UserPolicyConsts.DeletePolicy);
        await AddClaimIfMissingAsync(adminRole, existingClaims, UserPolicyConsts.ReadPolicy);
        await AddClaimIfMissingAsync(adminRole, existingClaims, UserPolicyConsts.UpdatePolicy);
        await AddClaimIfMissingAsync(adminRole, existingClaims, UserPolicyConsts.ReadAllPolicy);
        await AddClaimIfMissingAsync(adminRole, existingClaims, UserPolicyConsts.ManagePreferenceCatalogPolicy);
        await AddClaimIfMissingAsync(adminRole, existingClaims, ExpenseReadPolicy);
        await AddClaimIfMissingAsync(adminRole, existingClaims, ExpenseWritePolicy);
        await AddClaimIfMissingAsync(adminRole, existingClaims, ExpenseDeletePolicy);
        await AddClaimIfMissingAsync(adminRole, existingClaims, ExpenseAdminReadPolicy);
        await AddClaimIfMissingAsync(adminRole, existingClaims, IncomeReadPolicy);
        await AddClaimIfMissingAsync(adminRole, existingClaims, IncomeWritePolicy);
        await AddClaimIfMissingAsync(adminRole, existingClaims, IncomeDeletePolicy);
        await AddClaimIfMissingAsync(adminRole, existingClaims, IncomeAdminReadPolicy);
        await AddClaimIfMissingAsync(adminRole, existingClaims, InvestmentsReadPolicy);
        await AddClaimIfMissingAsync(adminRole, existingClaims, InvestmentsWritePolicy);
        await AddClaimIfMissingAsync(adminRole, existingClaims, InvestmentsDeletePolicy);
        await AddClaimIfMissingAsync(adminRole, existingClaims, InvestmentsAdminReadPolicy);
        await AddClaimIfMissingAsync(adminRole, existingClaims, InvestmentsAdminManageCategoriesPolicy);
        await AddClaimIfMissingAsync(adminRole, existingClaims, DashboardReadPolicy);
    }

    private async Task ConfigureUserRolePermissions(Role userRole)
    {
        IList<Claim> existingClaims = await roleManager.GetClaimsAsync(userRole);

        await AddClaimIfMissingAsync(userRole, existingClaims, UserPolicyConsts.ReadPolicy);
        await AddClaimIfMissingAsync(userRole, existingClaims, UserPolicyConsts.UpdatePolicy);
        await AddClaimIfMissingAsync(userRole, existingClaims, ExpenseReadPolicy);
        await AddClaimIfMissingAsync(userRole, existingClaims, ExpenseWritePolicy);
        await AddClaimIfMissingAsync(userRole, existingClaims, ExpenseDeletePolicy);
        await AddClaimIfMissingAsync(userRole, existingClaims, IncomeReadPolicy);
        await AddClaimIfMissingAsync(userRole, existingClaims, IncomeWritePolicy);
        await AddClaimIfMissingAsync(userRole, existingClaims, IncomeDeletePolicy);
        await AddClaimIfMissingAsync(userRole, existingClaims, InvestmentsReadPolicy);
        await AddClaimIfMissingAsync(userRole, existingClaims, InvestmentsWritePolicy);
        await AddClaimIfMissingAsync(userRole, existingClaims, InvestmentsDeletePolicy);
        await AddClaimIfMissingAsync(userRole, existingClaims, DashboardReadPolicy);
    }

    private async Task AddClaimIfMissingAsync(Role role, IList<Claim> existingClaims, string claimType)
    {
        if (existingClaims.Any(c => c.Type == claimType))
        {
            return;
        }

        IdentityResult result = await roleManager.AddClaimAsync(role, new Claim(claimType, UserPolicyConsts.ManagePreferenceCatalogClaimValue));
        if (!result.Succeeded)
        {
            string errors = string.Join(", ", result.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Failed to add claim '{claimType}' to role '{role.Name}': {errors}");
        }
    }

    private async Task CreateUserAsync(string email, string password, string roleName)
    {
        IdentityUser? existingIdentityUser = await userManager.FindByEmailAsync(email);
        if (existingIdentityUser is not null)
        {
            logger.LogInformation("Identity user already exists, skipping creation");
            return;
        }

        var identityUser = new IdentityUser
        {
            Email = email,
            UserName = email
        };

        IdentityResult createResult = await userManager.CreateAsync(identityUser, password);
        if (!createResult.Succeeded)
        {
            string errors = string.Join(", ", createResult.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Failed to create user '{email}': {errors}");
        }

        IdentityResult roleResult = await userManager.AddToRoleAsync(identityUser, roleName);
        if (!roleResult.Succeeded)
        {
            string errors = string.Join(", ", roleResult.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Failed to assign role '{roleName}' to '{email}': {errors}");
        }

        // Also create the corresponding domain User so token generation can succeed
        string firstName = roleName == AdminRoleType.Admin.ToString() ? "Admin" : "User";
        string lastName = "User";

        Currency? defaultCurrency = await dbContext.Currencies.SingleOrDefaultAsync(currency => currency.IsActive && currency.IsDefault);
        Timezone? defaultTimezone = await dbContext.Timezones.SingleOrDefaultAsync(timezone => timezone.IsActive && timezone.IsDefault);

        if (defaultCurrency is null || defaultTimezone is null)
        {
            throw new InvalidOperationException("Default currency and timezone must exist before seeding users.");
        }

        var domainUser = User.Create(firstName, lastName, identityUser.Id, defaultCurrency.Code, defaultTimezone.IanaId);
        dbContext.Users.Add(domainUser);
    }
}

