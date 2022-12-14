using System;
using System.Linq;
using System.Threading.Tasks;
using Disqord;
using Disqord.Bot.Commands;
using Disqord.Bot.Commands.Application;
using Disqord.Rest;
using Housekeeper.Entities;
using Housekeeper.Extensions;
using Housekeeper.Services;
using Qmmands;

namespace Housekeeper.Commands.Modules;

[SlashGroup("job")]
public class JobModule : DiscordApplicationGuildModuleBase
{
    private readonly JobService _jobService;

    public JobModule(JobService jobService)
    {
        _jobService = jobService;
    }

    [SlashCommand("info")]
    public async Task<IResult> InfoAsync(Job job)
    {
        var nextUser = await _jobService.GetNextUserAsync(job);

        return Response(new LocalEmbed()
            .WithColor(3092790)
            .WithTitle($"Job information: {job.Name}")
            .AddField("Description", job.Description)
            .AddField("Frequency", $"{job.DaysOfWeek} {(job.WeekFrequency == 1 ? "of every week" : $"every {job.WeekFrequency} weeks")}")
            .AddField("Users", job.UserIds.Any() ? string.Join(", ", job.UserIds.Select(x => Mention.User(x))) : "No users added")
            .AddField("Next User", nextUser is null ? "N/A" : $"{nextUser.Mention} {Markdown.Timestamp(job.GetNextActivation(), Markdown.TimestampFormat.RelativeTime)}"));
    }

    [SlashCommand("list")]
    public async Task<IResult> ListAsync()
    {
        var jobs = await _jobService.GetAllJobsAsync();

        return Response(new LocalEmbed()
            .WithColor(3092790)
            .WithTitle("Jobs")
            .WithDescription(string.Join("\n", jobs.OrderBy(x => x.Id).Select(x => $"**{x.Id}:** {x.Name}"))));
    }

    [SlashCommand("add")]
    public Task AddAsync()
    {
        var modal = new LocalInteractionModalResponse()
            .WithCustomId("job:add")
            .WithTitle("New job")
            .WithComponents(
                LocalComponent.Row(LocalComponent.TextInput("name", "Name", TextInputComponentStyle.Short)),
                LocalComponent.Row(LocalComponent.TextInput("description", "Description", TextInputComponentStyle.Paragraph)),
                LocalComponent.Row(LocalComponent.TextInput("daysOfWeek", "Day(s) of week", TextInputComponentStyle.Short)
                    .WithPlaceholder("eg. Monday, Wednesday, Friday")),
                LocalComponent.Row(LocalComponent.TextInput("weekFrequency", "Every (n) weeks", TextInputComponentStyle.Short)
                    .WithPlaceholder("eg. 1 for every week, 2 for every other week")));

        return Context.Interaction.Response().SendModalAsync(modal);
    }

    [SlashCommand("modify")]
    public async Task ModifyAsync(Job job)
    {
        var modal = new LocalInteractionModalResponse()
            .WithCustomId($"job:modify:{job.Id}")
            .WithTitle($"Modifying job - {job.Name}")
            .WithComponents(
                LocalComponent.Row(LocalComponent.TextInput("name", "Name", TextInputComponentStyle.Short)
                    .WithPrefilledValue(job.Name)),
                LocalComponent.Row(LocalComponent.TextInput("description", "Description", TextInputComponentStyle.Paragraph)
                    .WithPrefilledValue(job.Description)),
                LocalComponent.Row(LocalComponent.TextInput("daysOfWeek", "Day(s) of week", TextInputComponentStyle.Short)
                    .WithPrefilledValue(job.DaysOfWeek.ToString())
                    .WithPlaceholder("eg. Monday, Wednesday, Friday")),
                LocalComponent.Row(LocalComponent.TextInput("weekFrequency", "Every (n) weeks", TextInputComponentStyle.Short)
                    .WithPrefilledValue(job.WeekFrequency.ToString())
                    .WithPlaceholder("eg. 1 for every week, 2 for every other week")));

        await Context.Interaction.Response().SendModalAsync(modal);
    }

    [SlashCommand("delete")]
    public async Task<IResult> DeleteAsync(Job job)
    {
        await _jobService.DeleteAsync(job);
        return Response($"Deleted {job.Name}.");
    }

    [SlashCommand("add-user")]
    public async Task<IResult> AddUserAsync(
        Job job,
        [RequireNotBot] IMember user)
    {
        if (job.UserIds.Contains(user.Id))
            return Response($"{user.Mention} is already added to {job.Name}.");

        job.UserIds.Add(user.Id);
        await _jobService.UpdateAsync(job);
        return Response($"{user.Mention} has been added to {job.Name}.");
    }

    [SlashCommand("remove-user")]
    public async Task<IResult> RemoveUserAsync(
        Job job,
        [RequireNotBot] IMember user)
    {
        if (!job.UserIds.Contains(user.Id))
            return Response($"{user.Mention} is not added to {job.Name}.");

        job.UserIds.Remove(user.Id);
        await _jobService.UpdateAsync(job);
        return Response($"{user.Mention} has been removed from {job.Name}.");
    }

    [SlashCommand("set-start")]
    public async Task<IResult> SetStartAsync(
        Job job,
        DateTime start)
    {
        job.StartOfFirstWeekMidday = start.StartOfWeekMidday();
        await _jobService.UpdateAsync(job);
        return Response($"The start date of {job.Name} has been set to {Markdown.Timestamp(job.StartOfFirstWeekMidday, Markdown.TimestampFormat.LongDateTime)}.\n" +
                        $"The next activation is now {Markdown.Timestamp(job.GetNextActivation(), Markdown.TimestampFormat.RelativeTime)}.");
    }

    [SlashCommand("set-next-user")]
    public async Task<IResult> SetNextUserAsync(
        Job job,
        [RequireNotBot] IMember user)
    {
        await _jobService.ValidateUsersAsync(job);

        if (!job.UserIds.Contains(user.Id))
            return Response($"{user.Mention} is not added to {job.Name}.");

        job.PreviousUserIndex = job.UserIds.IndexOf(user.Id) - 1;
        await _jobService.UpdateAsync(job);
        return Response($"The next user for {job.Name} has been set to {user.Mention}.");
    }
}
