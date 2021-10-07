#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs;

namespace GGroupp.Internal.Timesheet.Bot
{
    public sealed class PipelineDialogBuilder
    {
        public static PipelineDialogBuilder Start([AllowNull] string dialogId)
            =>
            new(dialogId, new(), new());

        private readonly string dialogId;

        private readonly List<Dialog> dialogs;

        private readonly List<WaterfallStep> steps;

        internal PipelineDialogBuilder([AllowNull] string dialogId, [AllowNull] List<Dialog> dialogs, [AllowNull] List<WaterfallStep> steps)
        {
            this.dialogId = dialogId ?? string.Empty;
            this.dialogs = dialogs ?? new();
            this.steps = steps ?? new();
        }

        public PipelineDialogBuilder AddWaterfallStepDialog(
            Dialog stepDialog, Func<Dialog, WaterfallStepContext, CancellationToken, Task<DialogTurnResult>> waterfallStep)
            =>
            InternalAddWaterfallStepDialog(
                stepDialog ?? throw new ArgumentNullException(nameof(stepDialog)), 
                waterfallStep ?? throw new ArgumentNullException(nameof(waterfallStep)));

        public PipelineDialogBuilder AddWaterfallStep(WaterfallStep waterfallStep)
            =>
            InternalAddWaterfallStep(
                waterfallStep ?? throw new ArgumentNullException(nameof(waterfallStep)));

        public Dialog Build()
        {
            var pipelineDialogs = new List<Dialog>
            {
                new WaterfallDialog($"{dialogId}.Internal.Waterfall", steps)
            };
            pipelineDialogs.AddRange(dialogs);

            return new ImplPipelineDialog(dialogId, pipelineDialogs);
        }

        private PipelineDialogBuilder InternalAddWaterfallStepDialog(
            Dialog stepDialog, Func<Dialog, WaterfallStepContext, CancellationToken, Task<DialogTurnResult>> waterfallStep)
            =>
            new(
                dialogId: dialogId,
                dialogs: Add(dialogs, stepDialog),
                steps: Add(steps, (context, token) => waterfallStep.Invoke(stepDialog, context, token)));

        private PipelineDialogBuilder InternalAddWaterfallStep(WaterfallStep waterfallStep)
            =>
            new(
                dialogId: dialogId,
                dialogs: dialogs,
                steps: Add(steps, waterfallStep));

        private static List<T> Add<T>(List<T> source, T item)
        {
            source.Add(item);
            return source;
        }
    }
}