using System.Threading;
using System.Threading.Tasks;

namespace GarageGroup.Internal.Timesheet;

internal interface IAgentMessageApi
{
    ValueTask<AgentMessage> SendMessageAsync(
        int telegramUpdateId,
        long telegramUserId,
        long telegramChatId,
        string text,
        string? locale,
        CancellationToken cancellationToken);
}
