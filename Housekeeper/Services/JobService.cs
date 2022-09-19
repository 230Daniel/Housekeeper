using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Disqord;
using Disqord.Bot.Hosting;
using Housekeeper.Database;
using Housekeeper.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Housekeeper.Services;

public partial class JobService : DiscordBotService
{
    private new HousekeeperBot Bot => (HousekeeperBot) base.Bot;

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly SemaphoreSlim _semaphore;
    private List<Job> _jobs;

    public JobService(IConfiguration config, IServiceScopeFactory scopeFactory)
    {
        _todoChannelId = Snowflake.Parse(config["Discord:TodoChannelId"]);
        _scopeFactory = scopeFactory;
        _semaphore = new(0, 1);
        _scheduledJobs = new();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DatabaseContext>();

        _jobs = await db.Jobs.ToListAsync(stoppingToken);

        _semaphore.Release();

        await Bot.WaitUntilReadyAsync(stoppingToken);

        var activationTask = ActivateJobsAsync(stoppingToken);

        try { await Task.Delay(-1, stoppingToken); }
        catch (TaskCanceledException) { }

        _activatorCts.Cancel();
        await activationTask;
    }

    public async Task<ReadOnlyCollection<Job>> GetAllJobsAsync()
    {
        await _semaphore.WaitAsync();

        try
        {
            return _jobs.AsReadOnly();
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<Job> GetByIdAsync(int jobId)
    {
        await _semaphore.WaitAsync();

        try
        {
            return _jobs.FirstOrDefault(x => x.Id == jobId);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<IEnumerable<Job>> GetByNameAsync(string jobName)
    {
        await _semaphore.WaitAsync();

        try
        {
            return _jobs.Where(x => string.Equals(x.Name, jobName, StringComparison.InvariantCultureIgnoreCase));
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task AddAsync(Job job)
    {
        await _semaphore.WaitAsync();

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<DatabaseContext>();

            _jobs.Add(job);
            db.Jobs.Add(job);
            await db.SaveChangesAsync();

            Schedule(job);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task UpdateAsync(Job job)
    {
        await _semaphore.WaitAsync();

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<DatabaseContext>();

            db.Jobs.Update(job);
            await db.SaveChangesAsync();

            Schedule(job);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task DeleteAsync(Job job)
    {
        await _semaphore.WaitAsync();

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<DatabaseContext>();

            _jobs.Remove(job);
            db.Jobs.Remove(job);
            await db.SaveChangesAsync();

            Unschedule(job);
        }
        finally
        {
            _semaphore.Release();
        }
    }
}
