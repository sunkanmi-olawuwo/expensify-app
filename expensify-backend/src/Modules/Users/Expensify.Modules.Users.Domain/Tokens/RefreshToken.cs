using Expensify.Common.Domain;
using Expensify.Modules.Users.Domain.Users;

namespace Expensify.Modules.Users.Domain.Tokens;

public sealed class RefreshToken : Entity<string>
{
    private RefreshToken()
    {
    }

    public string JwtId { get; set; } = null!;

    public DateTime ExpiryDate { get; set; }

    public bool Invalidated { get; set; }

    public Guid UserId { get; set; }

    public User User { get; set; } = null!;

    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }

    public static RefreshToken Create(string token, string jwtId, Guid userId)
    {
        return new RefreshToken
        {
            Id = token,
            JwtId = jwtId,
            ExpiryDate = DateTime.UtcNow.AddDays(7),
            UserId = userId,
            CreatedAtUtc = DateTime.UtcNow
        };
    }

    public static void Invalidate(RefreshToken refreshToken)
    {
        refreshToken.Invalidated = true;
        refreshToken.UpdatedAtUtc = DateTime.UtcNow;
    }
}
