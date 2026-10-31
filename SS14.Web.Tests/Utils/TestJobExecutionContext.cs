#nullable enable
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Quartz;

namespace SS14.Web.Tests.Utils;

public sealed class TestJobExecutionContext : IJobExecutionContext
{
    public CancellationToken CancellationToken => CancellationToken.None;

    public object? Result { get; set; }

    public IScheduler Scheduler => throw new NotSupportedException();
    public ITrigger Trigger => throw new NotSupportedException();
    public ICalendar? Calendar => throw new NotSupportedException();
    public bool Recovering => false;
    public TriggerKey RecoveringTriggerKey => throw new NotSupportedException();
    public int RefireCount => 0;
    public JobDataMap MergedJobDataMap => new();
    public IJobDetail JobDetail => throw new NotSupportedException();
    public IJob JobInstance => throw new NotSupportedException();
    public DateTimeOffset FireTimeUtc => DateTimeOffset.UtcNow;
    public DateTimeOffset? ScheduledFireTimeUtc => DateTimeOffset.UtcNow;
    public DateTimeOffset? PreviousFireTimeUtc => null;
    public DateTimeOffset? NextFireTimeUtc => null;
    public string FireInstanceId => Guid.NewGuid().ToString();
    public object? RecoveringTriggerData => null;
    public TimeSpan JobRunTime => TimeSpan.Zero;
    public IDictionary<object, object> Data => new Dictionary<object, object>();

    public Task SchedulerFailedOver()
    {
        return Task.CompletedTask;
    }

    public TimeSpan? RemainingTime => null;
    public void Put(object key, object objectValue)
    {
        Data[key] = objectValue;
    }

    public object? Get(object key)
    {
        return Data.TryGetValue(key, out var value) ? value : null;
    }
}
