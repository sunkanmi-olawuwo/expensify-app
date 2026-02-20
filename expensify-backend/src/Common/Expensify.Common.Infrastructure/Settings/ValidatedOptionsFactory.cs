using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Expensify.Common.Application.Settings;

namespace Expensify.Common.Infrastructure.Settings;

public static class ValidatedOptionsFactory
{
    /// <summary>
    /// A factory for creating validated options based on configuration during startup.
    /// This is required because the default options mechanism requires the options to be
    /// registered with the DI container. This is not useful when you want the validated
    /// options used to configure the DI container during application startup.
    /// </summary>
    public static TOptions Create<TOptions>(IConfiguration configuration)
        where TOptions : IValidatedOptions<TOptions>, new()
    {
        var options = new TOptions();
        string sectionName = options.GetSectionName();
        configuration.GetSection(sectionName).Bind(options);
        IValidator<TOptions> validator = options.GetValidator();
        ValidationResult result = validator.Validate(options);
        if (!result.IsValid)
        {
            throw new Exception(result.ToString());
        }
        return options;
    }

    public static OptionsBuilder<TOptions> ValidatedOptions<TOptions>(this OptionsBuilder<TOptions> builder)
        where TOptions : class, IValidatedOptions<TOptions>, new()
    {
        builder.Services.AddSingleton<IValidateOptions<TOptions>>(new ValidatedOptions<TOptions>(builder.Name));

        return builder;
    }
}
