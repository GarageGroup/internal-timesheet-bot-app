using System.Threading;
using System.Threading.Tasks;

namespace GarageGroup.Internal.Timesheet;

internal interface IAgentProfileApi
{
    ValueTask<AgentProfile> GetProfileAsync(long telegramUserId, long telegramChatId, CancellationToken cancellationToken);
}
