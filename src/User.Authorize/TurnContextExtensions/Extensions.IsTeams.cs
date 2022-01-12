using Microsoft.Bot.Builder;
using Microsoft.Bot.Connector;

namespace GGroupp.Internal.Timesheet;

partial class TurnContextExtensions
{
    internal static bool IsTeamsChannel(this ITurnContext context)
        =>
        context.Activity.ChannelId.EqualsInvariant(Channels.Msteams);
}