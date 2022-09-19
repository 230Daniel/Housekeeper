using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Disqord;
using Disqord.Bot.Commands.Components;
using Disqord.Gateway;
using Disqord.Rest;
using Housekeeper.Entities;
using Housekeeper.Extensions;
using Housekeeper.Services;
using Microsoft.Extensions.Configuration;
using Qmmands;

namespace Housekeeper.Commands.Modules;

public class JobComponentModule : DiscordComponentGuildModuleBase
{
    private new HousekeeperBot Bot => (HousekeeperBot) base.Bot;

    private readonly Snowflake _doneChannelId;
    private readonly JobService _jobService;

    public JobComponentModule(IConfiguration config, JobService jobService)
    {
        _doneChannelId = Snowflake.Parse(config["Discord:DoneChannelId"]);
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

    [ButtonCommand("job:done")]
    public async Task<IResult> DoneAsync()
    {
        var message = (Context.Interaction as IComponentInteraction).Message;
        var userMention = message.Content;

        if (userMention != Context.Interaction.Author.Mention)
            return Response(new LocalInteractionMessageResponse()
                .WithContent($"Only {userMention} can mark this job as done.")
                .WithIsEphemeral());

        var jobName = message.Embeds[0].Title;

        await message.DeleteAsync();

        await (Bot.GetConfiguredGuild().GetChannel(_doneChannelId) as ITextChannel).SendMessageAsync(
            new LocalMessage()
                .WithContent($"{userMention} completed {jobName}.")
                .WithAllowedMentions(LocalAllowedMentions.None));

        return Results.Success;
    }
}
