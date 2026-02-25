using Expensify.Common.Domain;

namespace Expensify.Modules.Users.Domain.Users;

public sealed class User : Entity<Guid>, IAuditableEntity
{
    public const string DefaultCurrency = "GBP";
    public const string DefaultTimezone = "UTC";
    public const int DefaultMonthStartDay = 1;

    private User()
    {
    }

    public string FirstName { get; private set; }

    public string LastName { get; private set; }

    public string IdentityId { get; private set; }

    public string Currency { get; private set; }

    public string Timezone { get; private set; }

    public int MonthStartDay { get; private set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime? UpdatedAtUtc { get; set; }

    public static User Create(
        string firstName,
        string lastName,
        string identityId,
        string currency = DefaultCurrency,
        string timezone = DefaultTimezone,
        int monthStartDay = DefaultMonthStartDay)
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            FirstName = firstName,
            LastName = lastName,
            IdentityId = identityId,
            Currency = currency,
            Timezone = timezone,
            MonthStartDay = monthStartDay,
        };

        user.Raise(new UserRegisteredDomainEvent(user.Id));

        return user;
    }

    public void Update(string firstName, string lastName)
    {
        Update(firstName, lastName, Currency, Timezone, MonthStartDay);
    }

    public void Update(string firstName, string lastName, string currency, string timezone, int monthStartDay)
    {
        if (FirstName == firstName &&
            LastName == lastName &&
            Currency == currency &&
            Timezone == timezone &&
            MonthStartDay == monthStartDay)
        {
            return;
        }

        bool namesChanged = FirstName != firstName || LastName != lastName;
        FirstName = firstName;
        LastName = lastName;
        Currency = currency;
        Timezone = timezone;
        MonthStartDay = monthStartDay;

        if (namesChanged)
        {
            Raise(new UserProfileUpdatedDomainEvent(Id, FirstName, LastName));
        }
    }
}
