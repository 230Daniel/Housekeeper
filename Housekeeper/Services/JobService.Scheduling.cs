using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Housekeeper.Entities;

namespace Housekeeper.Services;

public partial class JobService
{
    private List<ScheduledJob> _scheduledJobs;
    private CancellationTokenSource _activatorCts;

    private async Task ActivateJobsAsync(CancellationToken stoppingToken)
    {
        lock (_scheduledJobs)
        {
            foreach (var job in _jobs)
            {
                var scheduledJob = new ScheduledJob(job, job.GetNextActivation());
                _scheduledJobs.Add(scheduledJob);
            }
        }

        _activatorCts = new CancellationTokenSource();

        while (!stoppingToken.IsCancellationRequested)
        {
            ScheduledJob nextJob;
            lock (_scheduledJobs)
            {
                nextJob = _scheduledJobs.Any()
                    ? _scheduledJobs.MinBy(x => x.ActivationTime)
                    : null;
            }
            try
            {
                if (nextJob is null)
                    await Task.Delay(-1, _activatorCts.Token);

                var delay = nextJob.ActivationTime - DateTime.UtcNow;
                if (delay.Ticks > 0)
                    await Task.Delay(delay, _activatorCts.Token);

                // activate job
            }
            catch (TaskCanceledException) { }
        }
    }

    private void Schedule(Job job)
    {
        lock (_scheduledJobs)
        {
            _scheduledJobs.RemoveAll(x => x.Job == job);

            var scheduledJob = new ScheduledJob(job, job.GetNextActivation());
            _scheduledJobs.Add(scheduledJob);

            RefreshActivator();
        }
    }

    private void Unschedule(Job job)
    {
        lock (_scheduledJobs)
        {
            _scheduledJobs.RemoveAll(x => x.Job == job);
            RefreshActivator();
        }
    }

    private void RefreshActivator()
    {
        var oldCts = _activatorCts;
        _activatorCts = new CancellationTokenSource();
        oldCts.Cancel();
    }

    private record ScheduledJob(Job Job, DateTime ActivationTime);
}
