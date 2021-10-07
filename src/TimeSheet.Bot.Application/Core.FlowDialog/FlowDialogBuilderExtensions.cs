#nullable enable

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Schema;

namespace GGroupp.Internal.Timesheet.Bot
{
    public static class FlowDialogBuilderExtensions
    {
        public static FlowDialogBuilder<TState> On<TState>(
            this FlowDialogBuilder<TState> flowDialogBuilder,
            Func<IFlowStateTurnContext<TState>, CancellationToken, Task> onAsync)
            where TState : class
            =>
            flowDialogBuilder.Next(
                async (context, token) =>
                {
                    await onAsync.Invoke(context, token).ConfigureAwait(false);
                    return FlowDialogAction.Next(context.State);
                });

        public static FlowDialogBuilder<TState> SendActivity<TState>(
            this FlowDialogBuilder<TState> flowDialogBuilder,
            Func<IFlowStateTurnContext<TState>, IActivity> activityFactory)
            where TState : class
            =>
            flowDialogBuilder.Next(
                async (context, token) =>
                {
                    var activity = activityFactory.Invoke(context);
                    _ = await context.SendActivityAsync(activity, token).ConfigureAwait(false);

                    return FlowDialogAction.Next(context.State);
                });

        public static FlowDialogBuilder<TState> SendActivity<TState>(
            this FlowDialogBuilder<TState> flowDialogBuilder,
            Func<IActivity> activityFactory)
            where TState : class
            =>
            flowDialogBuilder.Next(
                async (context, token) =>
                {
                    var activity = activityFactory.Invoke();
                    _ = await context.SendActivityAsync(activity, token).ConfigureAwait(false);

                    return FlowDialogAction.Next(context.State);
                });

        public static FlowDialogBuilder<TState> Await<TState>(
            this FlowDialogBuilder<TState> flowDialogBuilder)
            where TState : class
            =>
            flowDialogBuilder.Next(
                context => FlowDialogAction.Wait(context.State));

        public static FlowDialogBuilder<TState> AwaitForResult<TState>(
            this FlowDialogBuilder<TState> flowDialogBuilder,
            Func<IFlowStateTurnContext<TState>, Result<TState, FlowDialogFailure>> next)
            where TState : class
            =>
            flowDialogBuilder.Await().Next(
                async (context, token) =>
                {
                    var result = next.Invoke(context);

                    if(result.IsSuccess)
                    {
                        return result.SuccessOrThrow().Pipe(FlowDialogAction.Next);
                    }

                    var flowDialogFailure = result.FailureOrThrow();
                    if(flowDialogFailure.FailureActivity is IActivity activity)
                    {
                        _ = await context.SendActivityAsync(activity, token).ConfigureAwait(false);
                    }

                    return flowDialogFailure.FailureCode == FlowDialogFailureCode.EndDialog
                        ? FlowDialogAction.Next(context.State)
                        : FlowDialogAction.RetryTurn(context.State);
                });
    }
}