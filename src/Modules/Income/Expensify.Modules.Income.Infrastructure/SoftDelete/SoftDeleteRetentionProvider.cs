using Microsoft.Extensions.Options;
using Expensify.Modules.Income.Application.Abstractions.SoftDelete;

namespace Expensify.Modules.Income.Infrastructure.SoftDelete;

internal sealed class SoftDeleteRetentionProvider(IOptions<SoftDeleteOptions> options) : ISoftDeleteRetentionProvider
{
    private readonly SoftDeleteOptions _options = options.Value;

    public int RetentionDays => _options.RetentionDays;
}