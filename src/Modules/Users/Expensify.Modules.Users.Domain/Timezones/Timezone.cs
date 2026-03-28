using System.ComponentModel.DataAnnotations.Schema;
using Expensify.Common.Domain;

namespace Expensify.Modules.Users.Domain.Timezones;

public sealed class Timezone : Entity<string>, IAuditableEntity
{
    private Timezone()
    {
    }

    [NotMapped]
    public string IanaId => Id;

    public string DisplayName { get; private set; }

    public bool IsActive { get; private set; }

    public bool IsDefault { get; private set; }

    public int SortOrder { get; private set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime? UpdatedAtUtc { get; set; }

    public static Timezone Create(
        string ianaId,
        string displayName,
        bool isActive,
        bool isDefault,
        int sortOrder)
    {
        return new Timezone
        {
            Id = NormalizeIanaId(ianaId),
            DisplayName = displayName.Trim(),
            IsActive = isActive,
            IsDefault = isDefault,
            SortOrder = sortOrder
        };
    }

    public void Update(
        string displayName,
        bool isActive,
        bool isDefault,
        int sortOrder)
    {
        DisplayName = displayName.Trim();
        IsActive = isActive;
        IsDefault = isDefault;
        SortOrder = sortOrder;
    }

    public void ClearDefault()
    {
        IsDefault = false;
    }

    private static string NormalizeIanaId(string ianaId) => ianaId.Trim();
}
