using System.Threading;
using System.Threading.Tasks;
using GGroupp.Infra.Bot.Builder;
using Microsoft.Bot.Builder;

namespace GGroupp.Internal.Timesheet;

partial class BotStartChatFlow
{
    internal static async ValueTask<TurnState> InvokeFlowAsync(this IBotContext context, CancellationToken token)
    {
        var activity = MessageFactory.Text("Привет! Это G-Timesheet бот!");
        _ = await context.TurnContext.SendActivityAsync(activity, token).ConfigureAwait(false);

        return TurnState.Completed;
    }
}