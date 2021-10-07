#nullable enable

namespace GGroupp.Internal.Timesheet.Bot
{
    public sealed record FlowDialogAction<TState>
    {
        public FlowDialogAction(TState state, FlowDialogActionCode actionCode)
        {
            State = state;
            ActionCode = actionCode;
        }

        public TState State { get; }

        public FlowDialogActionCode ActionCode { get; }
    }

    public static class FlowDialogAction
    {
        public static FlowDialogAction<TState> Next<TState>(TState state)
            =>
            new(state, FlowDialogActionCode.Next);

        public static FlowDialogAction<TState> Wait<TState>(TState state)
            =>
            new(state, FlowDialogActionCode.Wait);

        public static FlowDialogAction<TState> RetryTurn<TState>(TState state)
            =>
            new(state, FlowDialogActionCode.RetryTurn);

        public static FlowDialogAction<TState> EndDialog<TState>(TState state)
            =>
            new(state, FlowDialogActionCode.EndDialog);

        public static FlowDialogAction<TState> Create<TState>(TState state, FlowDialogActionCode actionCode)
            =>
            new(state, actionCode);
    }
}