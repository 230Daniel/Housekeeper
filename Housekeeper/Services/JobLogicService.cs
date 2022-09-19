using System.Collections.Generic;
using System.Threading.Tasks;
using Disqord;
using Disqord.Gateway;
using Housekeeper.Database;
using Housekeeper.Entities;

namespace Housekeeper.Services;

public class JobLogicService
{
    private readonly DatabaseContext _db;
    private readonly HousekeeperBot _bot;

    public JobLogicService(DatabaseContext db, HousekeeperBot bot)
    {
        _db = db;
        _bot = bot;
    }

    public async Task<IMember> GetNextUserAsync(Job job)
    {
        var users = await GetAndValidateUsersAsync(job);
        if (users.Count == 0) return null;

        var nextUserIndex = job.PreviousUserIndex + 1;
        if (nextUserIndex >= users.Count) nextUserIndex = 0;

        return users[nextUserIndex];
    }

    public async Task<List<IMember>> GetAndValidateUsersAsync(Job job)
    {
        var guild = _bot.GetConfiguredGuild();
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
            _db.Jobs.Update(job);
            await _db.SaveChangesAsync();
        }

        return users;
    }
}
