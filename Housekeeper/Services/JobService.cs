using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Disqord.Bot.Hosting;
using Housekeeper.Database;
using Housekeeper.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Housekeeper.Services;

public partial class JobService : DiscordBotService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly SemaphoreSlim _semaphore;
    protected new HousekeeperBot Bot => (HousekeeperBot) base.Bot;

    private List<Job> _jobs;

    public JobService(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
        _semaphore = new(0, 1);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DatabaseContext>();

        _jobs = await db.Jobs.ToListAsync(stoppingToken);

        _semaphore.Release();
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

            await RescheduleAsync(job);
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
        }
        finally
        {
            _semaphore.Release();
        }
    }
}
