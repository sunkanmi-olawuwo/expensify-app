namespace Expensify.Modules.Users.Domain.Timezones;

public interface ITimezoneRepository
{
    void Add(Timezone timezone);
    void Update(Timezone timezone);
    Task<Timezone?> GetByIdAsync(string ianaId, CancellationToken cancellationToken = default);
    Task<Timezone?> GetDefaultAsync(CancellationToken cancellationToken = default);
}
