using Microsoft.AspNetCore.Identity;

namespace Expensify.Modules.Users.Domain.Identity;

public class RoleClaim : IdentityRoleClaim<string>
{
    public Role Role { get; set; } = null!;
}
