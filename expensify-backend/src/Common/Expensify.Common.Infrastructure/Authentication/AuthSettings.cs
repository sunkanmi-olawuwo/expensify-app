using FluentValidation;
using Expensify.Common.Application.Settings;

namespace Expensify.Common.Infrastructure.Authentication;


public sealed class AuthSettings : IValidatedOptions<AuthSettings>
{
    public const string SectionName = "AuthSettings";

    public static string GetSectionName() => SectionName;

    string IValidatedOptions<AuthSettings>.GetSectionName() => GetSectionName();

    public string Key { get; init; }
    public string Issuer { get; init; }
    public string Audience { get; init; }

    IValidator<AuthSettings> IValidatedOptions<AuthSettings>.GetValidator() => new Validator();

    public class Validator : AbstractValidator<AuthSettings>
    {
        public Validator()
        {
            RuleFor(x => x.Key)
                .NotEmpty().WithMessage("Auth Key must be provided.");
            RuleFor(x => x.Issuer)
                .NotEmpty().WithMessage("Auth Issuer must be provided.");
            RuleFor(x => x.Audience)
                .NotEmpty().WithMessage("Auth Audience must be provided.");
        }
    }
}
