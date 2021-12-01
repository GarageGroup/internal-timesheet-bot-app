using System.Threading;
using System.Threading.Tasks;
using GGroupp.Infra.Bot.Builder;

namespace GGroupp.Internal.Timesheet;

public static class BotStartBotBuilder
{
    public static IBotBuilder UseBotStart(this IBotBuilder bot)
        =>
        bot.Use(InvokeAsync);

    private static ValueTask<TurnState> InvokeAsync(IBotContext context, CancellationToken token)
        =>
        context.RecognizeCommandOrAbsent().FoldValueAsync(
            ctx => ctx.InvokeFlowAsync(token),
            CreateCompletedStateValueTask);

    private static ValueTask<TurnState> CreateCompletedStateValueTask()
        =>
        ValueTask.FromResult(TurnState.Completed);
}