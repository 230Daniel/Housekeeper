using System.Threading.Tasks;
using Disqord.Bot.Hosting;
using Disqord.Gateway;

namespace Housekeeper.Services;

public class MemberCacheService : DiscordBotService
{
    protected override async ValueTask OnReady(ReadyEventArgs e)
    {
        var guild = ((HousekeeperBot)Bot).GetConfiguredGuild();
        await Bot.Chunker.ChunkAsync(guild);
    }
}
