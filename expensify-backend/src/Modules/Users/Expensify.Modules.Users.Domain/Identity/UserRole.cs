using Microsoft.AspNetCore.Identity;

namespace Expensify.Modules.Users.Domain.Identity;

public class UserRole : IdentityUserRole<string>
{
    public Role Role { get; set; } = null!;
}
