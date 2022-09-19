using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Disqord;
using Disqord.Bot.Commands.Components;
using Housekeeper.Entities;
using Housekeeper.Extensions;
using Housekeeper.Services;
using Qmmands;

namespace Housekeeper.Commands.Modules;

public class JobModalModule : DiscordComponentGuildModuleBase
{
    private readonly JobService _jobService;

    public JobModalModule(JobService jobService)
    {
        _jobService = jobService;
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

        await _jobService.AddAsync(job);
        return Response($"Created job {job.Name} with ID {job.Id}.");
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

        await _jobService.UpdateAsync(job);
        return Response(new LocalInteractionMessageResponse()
            .WithContent($"Modified {name}."));
    }
}
