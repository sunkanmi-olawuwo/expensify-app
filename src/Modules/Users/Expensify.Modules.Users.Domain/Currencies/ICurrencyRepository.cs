namespace Expensify.Modules.Users.Domain.Currencies;

public interface ICurrencyRepository
{
    void Add(Currency currency);
    void Update(Currency currency);
    Task<Currency?> GetByIdAsync(string code, CancellationToken cancellationToken = default);
    Task<Currency?> GetDefaultAsync(CancellationToken cancellationToken = default);
}
