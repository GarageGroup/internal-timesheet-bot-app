using Microsoft.Bot.Connector;
using Microsoft.Bot.Schema;

namespace GGroupp.Internal.Timesheet;

partial class OAuthActivityExtensions
{
    internal static bool IsTeams(this Activity activity)
        =>
        activity.ChannelId.EqualsInvariant(Channels.Msteams);
}