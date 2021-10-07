#nullable enable

using System;

namespace GGroupp.Internal.Timesheet.Bot
{
    public static class FlowDialog
    {
        public static FlowDialogBuilder<TState> Start<TState>(string dialogId, TState defaultState)
            where TState : class
            =>
            new(dialogId, defaultState, Array.Empty<FlowDialogStep<object, object>>());

        public static FlowDialogBuilder<TState> Start<TState>(string dialogId)
            where TState : class, new()
            =>
            new(dialogId, new TState(), Array.Empty<FlowDialogStep<object, object>>());
    }
}