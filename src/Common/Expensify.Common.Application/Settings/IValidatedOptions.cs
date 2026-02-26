using FluentValidation;

namespace Expensify.Common.Application.Settings;

public interface IValidatedOptions<in TOptions>
    where TOptions : new()
{
    string GetSectionName();

    IValidator<TOptions> GetValidator();
}
