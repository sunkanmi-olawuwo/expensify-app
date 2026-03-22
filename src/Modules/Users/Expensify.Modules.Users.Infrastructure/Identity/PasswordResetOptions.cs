using FluentValidation;
using Expensify.Common.Application.Settings;

namespace Expensify.Modules.Users.Infrastructure.Identity;

public sealed class PasswordResetOptions : IValidatedOptions<PasswordResetOptions>
{
    public const string SectionName = "Users:PasswordReset";

    public string ApiKey { get; init; } = string.Empty;
    public string FromEmail { get; init; } = string.Empty;
    public string FromName { get; init; } = string.Empty;
    public string ResetUrlBase { get; init; } = string.Empty;

    string IValidatedOptions<PasswordResetOptions>.GetSectionName() => SectionName;

    IValidator<PasswordResetOptions> IValidatedOptions<PasswordResetOptions>.GetValidator() => new Validator();

    public sealed class Validator : AbstractValidator<PasswordResetOptions>
    {
        public Validator()
        {
            RuleFor(x => x.ApiKey)
                .NotEmpty()
                .WithMessage("Users password reset API key must be provided.");

            RuleFor(x => x.FromEmail)
                .NotEmpty()
                .WithMessage("Users password reset from email must be provided.")
                .EmailAddress()
                .WithMessage("Users password reset from email must be a valid email address.");

            RuleFor(x => x.FromName)
                .NotEmpty()
                .WithMessage("Users password reset from name must be provided.");

            RuleFor(x => x.ResetUrlBase)
                .NotEmpty()
                .WithMessage("Users password reset URL base must be provided.")
                .Must(BeAbsoluteUri)
                .WithMessage("Users password reset URL base must be an absolute URL.");
        }

        private static bool BeAbsoluteUri(string resetUrlBase)
        {
            return Uri.TryCreate(resetUrlBase, UriKind.Absolute, out Uri? uri) &&
                   (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
        }
    }
}
