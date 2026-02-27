using Microsoft.Extensions.Options;
using Quartz;

namespace Expensify.Modules.Expenses.Infrastructure.SoftDelete;

internal sealed class ConfigureProcessSoftDeletePurgeJob(IOptions<SoftDeleteOptions> softDeleteOptions)
    : IConfigureOptions<QuartzOptions>
{
    private readonly SoftDeleteOptions _softDeleteOptions = softDeleteOptions.Value;

    public void Configure(QuartzOptions options)
    {
        string jobName = typeof(ProcessSoftDeletePurgeJob).FullName!;

        options
            .AddJob<ProcessSoftDeletePurgeJob>(configure => configure.WithIdentity(jobName))
            .AddTrigger(configure =>
                configure
                    .ForJob(jobName)
                    .WithSimpleSchedule(schedule =>
                        schedule.WithIntervalInSeconds(_softDeleteOptions.IntervalInSeconds).RepeatForever()));
    }
}