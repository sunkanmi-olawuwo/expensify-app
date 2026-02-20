namespace Expensify.Modules.Users.Domain.Tokens;

public interface IRefreshTokenRepository
{
    void Add(RefreshToken refreshToken);
    void Update(RefreshToken refreshToken);
    void Remove(RefreshToken refreshToken);
    Task<RefreshToken?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
    Task<List<RefreshToken>> GetValidTokenAsync(Guid userId, CancellationToken cancellationToken = default);
}
