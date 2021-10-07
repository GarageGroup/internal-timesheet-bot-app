#nullable enable

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;

namespace GGroupp.Internal.Timesheet.Bot
{
    public sealed class PipelineDialogBuilder<T>
        where T : new()
    {
        private readonly PipelineDialogBuilder builder;

        internal PipelineDialogBuilder(PipelineDialogBuilder builder)
            =>
            this.builder = builder;

        public PipelinePromptBuilder<T, TResult> NextPrompt<TResult>(
            Prompt<TResult> prompt, PromptOptions promptOptions)
            =>
            builder.AddWaterfallStepDialog(
                prompt, (_, context, token) => context.PromptAsync(prompt.Id, promptOptions, token))
            .Pipe(
                b => new PipelinePromptBuilder<T, TResult>(b));

        public PipelinePromptBuilder<T, TResult> NextPrompt<TResult>(
            Prompt<TResult> prompt, Func<ITurnContext, PromptOptions> promptOptionsProvider)
            =>
            builder.AddWaterfallStepDialog(
                prompt,
                (_, context, token) => context.PromptAsync(prompt.Id, promptOptionsProvider.Invoke(context.Context), token))
            .Pipe(
                b => new PipelinePromptBuilder<T, TResult>(b));

        public PipelinePromptBuilder<T, string> NextTextPrompt(
            string promptDialogId, PromptOptions promptOptions)
            =>
            NextPrompt(new TextPrompt(promptDialogId), promptOptions);

        public PipelinePromptBuilder<T, string> NextTextPrompt(
            string promptDialogId, PromptValidator<string> validator, PromptOptions promptOptions)
            =>
            NextPrompt(new TextPrompt(promptDialogId, validator), promptOptions);

        public PipelinePromptBuilder<T, TNumber> NextNumberPrompt<TNumber>(
            string promptDialogId, PromptOptions promptOptions)
            where TNumber : struct
            =>
            NextPrompt(new NumberPrompt<TNumber>(promptDialogId), promptOptions);

        public PipelinePromptBuilder<T, TNumber> NextNumberPrompt<TNumber>(
            string promptDialogId, PromptValidator<TNumber> validator, PromptOptions promptOptions)
            where TNumber : struct
            =>
            NextPrompt(new NumberPrompt<TNumber>(promptDialogId, validator), promptOptions);

        public PipelinePromptBuilder<T, TNumber> NextNumberPrompt<TNumber>(
            string promptDialogId, Func<PromptValidatorContext<TNumber>, bool> validator, PromptOptions promptOptions)
            where TNumber : struct
            =>
            NextPrompt(
                new NumberPrompt<TNumber>(promptDialogId, (context, _) => validator.Invoke(context).Pipe(Task.FromResult)),
                promptOptions);

        public PipelinePromptBuilder<T, TValue> NextAdaptiveCardPrompt<TValue>(
            string promptDialogId, Func<ITurnContext, PromptOptions> promptOptionsProvider)
            =>
            NextPrompt(new AdaptiveCardPrompt<TValue>(promptDialogId), promptOptionsProvider);

        public PipelinePromptBuilder<T, TValue> NextAdaptiveCardPrompt<TValue>(
            string promptDialogId, PromptValidator<TValue> validator, Func<ITurnContext, PromptOptions> promptOptionsProvider)
            =>
            NextPrompt(new AdaptiveCardPrompt<TValue>(promptDialogId, validator), promptOptionsProvider);

        public PipelinePromptBuilder<T, TValue> NextAdaptiveCardPrompt<TValue>(
            string promptDialogId,
            Func<PromptValidatorContext<TValue>, bool> validator,
            Func<ITurnContext, PromptOptions> promptOptionsProvider)
            =>
            NextPrompt(
                new AdaptiveCardPrompt<TValue>(promptDialogId, (context, _) => validator.Invoke(context).Pipe(Task.FromResult)),
                promptOptionsProvider);

        public PipelineDialogBuilder<T> Next(
            Func<ITurnContext, T, CancellationToken, Task> nextAsync)
            =>
            builder.AddWaterfallStep(
                async (waterfallContext, token) =>
                {
                    var value = waterfallContext.GetWaterfallCurrentValue<T>();
                    await nextAsync.Invoke(waterfallContext.Context, value, token);
                    return await waterfallContext.NextAsync(value, token);
                })
            .Pipe(
                b => new PipelineDialogBuilder<T>(b));

        public Dialog End()
            =>
            builder.AddWaterfallStep(
                (waterfallContext, _) => waterfallContext.EndDialogAsync())
            .Build();
    }
}