using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Disqord;
using Disqord.Bot.Commands.Components;
using Housekeeper.Database;
using Housekeeper.Entities;
using Housekeeper.Extensions;
using Qmmands;

namespace Housekeeper.Commands.Modules;

public class JobModalModule : DiscordComponentGuildModuleBase
{
    private readonly DatabaseContext _db;

    public JobModalModule(DatabaseContext db)
    {
        _db = db;
    }

    [ModalCommand("job:add")]
    public async Task<IResult> AddAsync(
        string name,
        string description,
        [Minimum(1)] DaysOfWeek daysOfWeek,
        [Minimum(1)] int weekFrequency)
    {
        var job = new Job
        {
            Name = name,
            Description = description,
            DaysOfWeek = daysOfWeek,
            WeekFrequency = weekFrequency,
            UserIds = new List<ulong>(),
            StartOfFirstWeekMidday = DateTime.UtcNow.StartOfWeekMidday(),
            PreviousUserIndex = -1
        };

        _db.Jobs.Add(job);
        await _db.SaveChangesAsync();

        return Response($"Created {name}.");
    }

    [ModalCommand("job:modify:*")]
    public async Task<IResult> ModifyAsync(
        Job job,
        string name,
        string description,
        [Minimum(1)] DaysOfWeek daysOfWeek,
        [Minimum(1)] int weekFrequency)
    {
        job.Name = name;
        job.Description = description;
        job.DaysOfWeek = daysOfWeek;
        job.WeekFrequency = weekFrequency;

        _db.Jobs.Update(job);
        await _db.SaveChangesAsync();

        return Response(new LocalInteractionMessageResponse()
            .WithContent($"Modified {name}."));
    }
}
