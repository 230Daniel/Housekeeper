using System;
using System.Threading;
using System.Threading.Tasks;
using Disqord;
using Disqord.Bot;
using Disqord.Bot.Commands;
using Disqord.Bot.Commands.Interaction;
using Disqord.Gateway;
using Housekeeper.Commands.TypeParsers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Qmmands;
using Qmmands.Default;

namespace Housekeeper.Services;

public class HousekeeperBot : DiscordBot
{
    public Snowflake GuildId { get; }

    public HousekeeperBot(
        IConfiguration config,
        IOptions<DiscordBotConfiguration> options,
        ILogger<HousekeeperBot> logger,
        IServiceProvider services,
        DiscordClient client)
        : base(options, logger, services, client)
    {
        GuildId = Snowflake.Parse(config["Discord:GuildId"]);
    }

    // Only process messages from the configured guild
    protected override ValueTask<bool> OnMessage(IGatewayUserMessage message)
        => ValueTask.FromResult(message.GuildId.HasValue && message.GuildId.Value == GuildId);

    // Only process interactions from the configured guild
    protected override ValueTask<bool> OnInteraction(IUserInteraction interaction)
        => ValueTask.FromResult(interaction.GuildId.HasValue && interaction.GuildId.Value == GuildId);

    // Override to send failure messages for IComponentInteraction and IModalSubmitInteraction
    protected override ValueTask OnFailedResult(IDiscordCommandContext context, IResult result)
    {
        if (context is IDiscordInteractionCommandContext { Interaction: IAutoCompleteInteraction })
            return default;

        var message = CreateFailureMessage(context);
        if (message is null)
            return default;

        if (!FormatFailureMessage(context, message, result))
            return default;

        return SendFailureMessageAsync(context, message);
    }

    protected override ValueTask AddTypeParsers(DefaultTypeParserProvider typeParserProvider, CancellationToken cancellationToken)
    {
        typeParserProvider.AddParser(new DateTimeTypeParser());
        typeParserProvider.AddParser(new JobTypeParser());
        return base.AddTypeParsers(typeParserProvider, cancellationToken);
    }

    public CachedGuild GetConfiguredGuild()
        => this.GetGuild(GuildId);
}
