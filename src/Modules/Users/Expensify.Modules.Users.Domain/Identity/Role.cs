using Microsoft.AspNetCore.Identity;

namespace Expensify.Modules.Users.Domain.Identity;

public class Role : IdentityRole
{
    public ICollection<UserRole> UserRoles { get; set; } = null!;
    public ICollection<RoleClaim> RoleClaims { get; set; } = null!;
}
