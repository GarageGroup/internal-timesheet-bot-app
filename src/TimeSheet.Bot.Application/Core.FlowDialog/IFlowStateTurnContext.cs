#nullable enable

using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;

namespace GGroupp.Internal.Timesheet.Bot
{
    public interface IFlowStateTurnContext<out TState> : ITurnContext
    {
        TState State { get; }

        DialogContext DialogContext { get; }
    }
}