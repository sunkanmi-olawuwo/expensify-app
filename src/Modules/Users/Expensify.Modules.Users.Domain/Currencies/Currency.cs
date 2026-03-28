using System.ComponentModel.DataAnnotations.Schema;
using Expensify.Common.Domain;

namespace Expensify.Modules.Users.Domain.Currencies;

public sealed class Currency : Entity<string>, IAuditableEntity
{
    private Currency()
    {
    }

    [NotMapped]
    public string Code => Id;

    public string Name { get; private set; }

    public string Symbol { get; private set; }

    public int MinorUnit { get; private set; }

    public bool IsActive { get; private set; }

    public bool IsDefault { get; private set; }

    public int SortOrder { get; private set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime? UpdatedAtUtc { get; set; }

    public static Currency Create(
        string code,
        string name,
        string symbol,
        int minorUnit,
        bool isActive,
        bool isDefault,
        int sortOrder)
    {
        return new Currency
        {
            Id = NormalizeCode(code),
            Name = name.Trim(),
            Symbol = symbol.Trim(),
            MinorUnit = minorUnit,
            IsActive = isActive,
            IsDefault = isDefault,
            SortOrder = sortOrder
        };
    }

    public void Update(
        string name,
        string symbol,
        int minorUnit,
        bool isActive,
        bool isDefault,
        int sortOrder)
    {
        Name = name.Trim();
        Symbol = symbol.Trim();
        MinorUnit = minorUnit;
        IsActive = isActive;
        IsDefault = isDefault;
        SortOrder = sortOrder;
    }

    public void ClearDefault()
    {
        IsDefault = false;
    }

    private static string NormalizeCode(string code) => code.Trim().ToUpperInvariant();
}
