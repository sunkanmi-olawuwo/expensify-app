using Bogus;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Expensify.Modules.Users.Domain.Policies;
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
    public async Task SeedUsersAsync()
    {
        SetRandomizerSeed();

        if (await dbContext.Users.AnyAsync())
        {
            logger.LogInformation("Users already exist, skipping user seeding");
            return;
        }

        logger.LogInformation("Starting user seeding...");

        Role adminRole = await EnsureRoleAsync(AdminRoleType.Admin.ToString());
        Role tutorRole = await EnsureRoleAsync(RoleType.Tutor.ToString());

        await ConfigureAdminRolePermissions(adminRole);
        await ConfigureTutorRolePermissions(tutorRole);

        await CreateUserAsync("admin@test.com", "Test1234!", AdminRoleType.Admin.ToString());
        await CreateUserAsync("tutor@test.com", "Test1234!", RoleType.Tutor.ToString());

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
    }

    private async Task ConfigureTutorRolePermissions(Role tutorRole)
    {
        IList<Claim> existingClaims = await roleManager.GetClaimsAsync(tutorRole);

        await AddClaimIfMissingAsync(tutorRole, existingClaims, UserPolicyConsts.ReadPolicy);
        await AddClaimIfMissingAsync(tutorRole, existingClaims, UserPolicyConsts.UpdatePolicy);
    }

    private async Task AddClaimIfMissingAsync(Role role, IList<Claim> existingClaims, string claimType)
    {
        if (existingClaims.Any(c => c.Type == claimType))
        {
            return;
        }

        await roleManager.AddClaimAsync(role, new Claim(claimType, "true"));
    }

    private async Task CreateUserAsync(string email, string password, string roleName)
    {
        IdentityUser? existingIdentityUser = await userManager.FindByEmailAsync(email);
        if (existingIdentityUser is not null)
        {
            logger.LogInformation("Identity user {Email} already exists, skipping creation", email);
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
        string firstName = roleName == AdminRoleType.Admin.ToString() ? "Admin" : "Tutor";
        string lastName = "User";

        var domainUser = User.Create(firstName, lastName, identityUser.Id);
        dbContext.Users.Add(domainUser);
    }
}
