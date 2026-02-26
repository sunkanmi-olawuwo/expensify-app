namespace Expensify.Modules.Users.Domain.Users;

public interface IUserRepository
{
    void Add(User user);
    void Update(User user);
    void Remove(User user);
    Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<User?> GetByIdentityIdAsync(string identityId, CancellationToken cancellationToken = default);
}
