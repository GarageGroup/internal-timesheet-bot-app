#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs;

namespace GGroupp.Internal.Timesheet.Bot
{
    public sealed class FlowDialogBuilder<TState>
        where TState : class
    {
        private readonly string dialogId;

        private readonly object defaultState;

        private readonly IReadOnlyCollection<FlowDialogStep<object, object>> dialogSteps;

        internal FlowDialogBuilder(string dialogId, object defaultState, [AllowNull] IReadOnlyCollection<FlowDialogStep<object, object>> dialogSteps)
        {
            this.dialogId = dialogId;
            this.defaultState = defaultState;
            this.dialogSteps = dialogSteps ?? Array.Empty<FlowDialogStep<object, object>>();
        }

        public FlowDialogBuilder<TNextState> Next<TNextState>(FlowDialogStep<TState, TNextState> nextStep)
            where TNextState : class
            =>
            InternalNext<TNextState>(
                nextStep ?? throw new ArgumentNullException(nameof(nextStep)));

        public FlowDialogBuilder<TNextState> Next<TNextState>(Func<IFlowStateTurnContext<TState>, FlowDialogAction<TNextState>> nextStep)
            where TNextState : class
            =>
            InternalNext<TNextState>(
                nextStep ?? throw new ArgumentNullException(nameof(nextStep)));

        public Dialog End() => new ImplFlowDialog(dialogId, defaultState, dialogSteps);

        private FlowDialogBuilder<TNextState> InternalNext<TNextState>(FlowDialogStep<TState, TNextState> nextStep)
            where TNextState : class
            =>
            new(
                dialogId: dialogId,
                defaultState: defaultState,
                dialogSteps: new List<FlowDialogStep<object, object>>(dialogSteps)
                {
                    (context, token) => InvokeAsync(nextStep, context, token)
                });

        private FlowDialogBuilder<TNextState> InternalNext<TNextState>(
            Func<IFlowStateTurnContext<TState>, FlowDialogAction<TNextState>> nextStep)
            where TNextState : class
            =>
            InternalNext(
                (state, _) => state.Pipe(nextStep).Pipe(Task.FromResult));

        private static async Task<FlowDialogAction<object>> InvokeAsync<TNextState>(
            FlowDialogStep<TState, TNextState> nextStep, IFlowStateTurnContext<object> context, CancellationToken token)
            where TNextState : class
            =>
            Pipeline.Pipe(
                await nextStep.Invoke(context.Pipe(MapTurnContext), token).ConfigureAwait(false))
            .Pipe(
                next => new FlowDialogAction<object>(next.State, next.ActionCode));

        private static IFlowStateTurnContext<TState> MapTurnContext(IFlowStateTurnContext<object> source)
            =>
            new ImplFlowStateTurnContext<TState>(source.DialogContext, source.State.Pipe(CastToStateTypeOrThrow));

        private static TState CastToStateTypeOrThrow(object? source)
            =>
            source is TState state
                ? state
                : throw CreateUnexpectedStateTypeException(source);

        private static InvalidOperationException CreateUnexpectedStateTypeException(object? source)
            =>
            new(FormattableString.Invariant($"State was expected to be {typeof(TState).Name} but it was {source?.GetType()?.Name ?? "null"}."));
    }
}