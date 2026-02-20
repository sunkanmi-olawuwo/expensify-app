using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Options;
using Expensify.Common.Application.Settings;

namespace Expensify.Common.Infrastructure.Settings;

/// <summary>
/// Implementation of <see cref="IValidateOptions{TOptions}"/>
/// </summary>
/// <typeparam name="TOptions">The options type to validate.</typeparam>
internal sealed class ValidatedOptions<TOptions> : IValidateOptions<TOptions> where TOptions : class, IValidatedOptions<TOptions>, new()
{
    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="name">Options name.</param>
    public ValidatedOptions(string? name)
    {
        Name = name;
    }

    /// <summary>
    /// The options name.
    /// </summary>
    public string? Name { get; }

    /// <summary>
    /// Validates a specific named options instance (or all when <paramref name="name"/> is null).
    /// </summary>
    /// <param name="name">The name of the options instance being validated.</param>
    /// <param name="options">The options instance.</param>
    /// <returns>The <see cref="ValidateOptionsResult"/> result.</returns>
    public ValidateOptionsResult Validate(string? name, TOptions options)
    {
        // null name is used to configure all named options
        if (Name == null || name == Name)
        {
            IValidator<TOptions> validator = options.GetValidator();
            ValidationResult result = validator.Validate(options);
            if (result.IsValid)
            {
                return ValidateOptionsResult.Success;
            }

            return ValidateOptionsResult.Fail(result.ToString());
        }

        // ignored if not validating this instance
        return ValidateOptionsResult.Skip;
    }
}
