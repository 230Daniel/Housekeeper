using System.Collections.Generic;
using System.Threading.Tasks;
using Disqord;
using Disqord.Gateway;
using Housekeeper.Database;
using Housekeeper.Entities;
using Microsoft.Extensions.DependencyInjection;

namespace Housekeeper.Services;

public partial class JobService
{
    public async Task<IMember> GetNextUserAsync(Job job)
    {
        var users = await ValidateUsersAsync(job);
        if (users.Count == 0) return null;

        var nextUserIndex = job.PreviousUserIndex + 1;
        if (nextUserIndex >= users.Count) nextUserIndex = 0;

        return users[nextUserIndex];
    }

    public async Task<List<IMember>> ValidateUsersAsync(Job job)
    {
        await _semaphore.WaitAsync();

        try
        {
            var guild = Bot.GetConfiguredGuild();

            var updated = false;
            var users = new List<IMember>();

            foreach (var userId in job.UserIds)
            {
                var member = guild.GetMember(userId);
                if (member is null)
                {
                    job.UserIds.Remove(userId);
                    updated = true;
                }
                else users.Add(member);
            }

            if (updated)
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<DatabaseContext>();

                db.Jobs.Update(job);
                await db.SaveChangesAsync();
            }

            return users;
        }
        finally
        {
            _semaphore.Release();
        }
    }
}
