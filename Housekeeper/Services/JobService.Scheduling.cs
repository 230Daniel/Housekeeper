using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Disqord;
using Disqord.Gateway;
using Disqord.Rest;
using Housekeeper.Entities;
using Microsoft.Extensions.Logging;

namespace Housekeeper.Services;

public partial class JobService
{
    private readonly Snowflake _todoChannelId;
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
                Logger.LogInformation("Waiting for {Delay} to trigger {Job}", delay, nextJob.Job.Name);
                if (delay.Ticks > 0)
                    await Task.Delay(delay, _activatorCts.Token);

                await ActivateJobAsync(nextJob.Job);
            }
            catch (TaskCanceledException) { }
        }
    }

    private async Task ActivateJobAsync(Job job)
    {
        var nextUser = await GetNextUserAsync(job);

        await (Bot.GetConfiguredGuild().GetChannel(_todoChannelId) as ITextChannel).SendMessageAsync(
            new LocalMessage()
                .WithContent(nextUser.Mention)
                .WithAllowedMentions(LocalAllowedMentions.ExceptEveryone)
                .AddEmbed(new LocalEmbed()
                    .WithColor(3092790)
                    .WithTitle(job.Name)
                    .WithDescription(job.Description))
                .AddComponent(LocalComponent.Row(LocalComponent.Button("job:done", "Done")
                    .WithStyle(LocalButtonComponentStyle.Success)))
        );

        job.PreviousUserIndex = job.UserIds.IndexOf(nextUser.Id);
        await UpdateAsync(job);
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
