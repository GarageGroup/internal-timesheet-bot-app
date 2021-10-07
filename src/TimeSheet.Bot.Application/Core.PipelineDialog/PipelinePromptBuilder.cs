#nullable enable

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;

namespace GGroupp.Internal.Timesheet.Bot
{
    public sealed class PipelinePromptBuilder<T, TPromptResult>
        where T : new()
    {
        private readonly PipelineDialogBuilder builder;

        internal PipelinePromptBuilder(PipelineDialogBuilder builder)
            =>
            this.builder = builder;

        public PipelinePromptBuilder<T, TPromptResult> OnPromptResult(
            Func<ITurnContext, TPromptResult, CancellationToken, Task> onAsync)
            =>
            builder.AddWaterfallStep(
                async (waterfallContext, token) =>
                {
                    var promptResult = (TPromptResult)waterfallContext.Result;
                    await onAsync.Invoke(waterfallContext.Context, promptResult, token);

                    return await waterfallContext.NextAsync(promptResult, token);
                })
            .Pipe(
                b => new PipelinePromptBuilder<T, TPromptResult>(b));

        public PipelineDialogBuilder<T> WithPromptResult(
            Func<T, TPromptResult, T> next)
            =>
            builder.AddWaterfallStep(
                (waterfallContext, token) =>
                {
                    var currentValue = waterfallContext.GetWaterfallCurrentValue<T>();

                    var promptResult = (TPromptResult)waterfallContext.Result;
                    var newValue = next.Invoke(currentValue, promptResult);

                    _ = waterfallContext.SaveWaterfallCurrentValue(newValue);
                    return waterfallContext.NextAsync(promptResult, token);
                })
            .Pipe(
                b => new PipelineDialogBuilder<T>(b));
    }
}