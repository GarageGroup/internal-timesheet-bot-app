#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;

namespace GGroupp.Internal.Timesheet.Bot
{
    internal sealed class ImplFlowDialog : Dialog
    {
        private const string PersistedState = "_state";

        private const string PersistedStepIndex = "_index";

        private readonly FlowDialogStep<object, object>[] dialogSteps;

        private readonly object defaultValue;

        public ImplFlowDialog(string dialogId, object defaultValue, [AllowNull] IReadOnlyCollection<FlowDialogStep<object, object>> dialogSteps)
            : base(dialogId)
        {
            this.defaultValue = defaultValue;
            this.dialogSteps = dialogSteps?.ToArray() ?? Array.Empty<FlowDialogStep<object, object>>();
        }

        public override Task<DialogTurnResult> BeginDialogAsync(
            DialogContext dialogContext, object options, CancellationToken cancellationToken = default)
        {
            _ = dialogContext ?? throw new ArgumentNullException(nameof(dialogContext));

            if(cancellationToken.IsCancellationRequested)
            {
                return Task.FromCanceled<DialogTurnResult>(cancellationToken);
            }

            var dialogState = dialogContext.ActiveDialog.State;
            var currentState = options ?? defaultValue;

            dialogState[PersistedState] = currentState;
            dialogState[PersistedStepIndex] = 0;

            return ExecuteDialogAsync(dialogContext, cancellationToken);
        }

        public override Task<DialogTurnResult> ContinueDialogAsync(DialogContext dialogContext, CancellationToken cancellationToken = default)
        {
            _ = dialogContext ?? throw new ArgumentNullException(nameof(dialogContext));

            if(cancellationToken.IsCancellationRequested)
            {
                return Task.FromCanceled<DialogTurnResult>(cancellationToken);
            }

            if (dialogContext.Context.Activity.Type != ActivityTypes.Message)
            {
                return Task.FromResult(EndOfTurn);
            }

            return ExecuteDialogAsync(dialogContext, cancellationToken);
        }

        private async Task<DialogTurnResult> ExecuteDialogAsync(DialogContext dialogContext, CancellationToken cancellationToken)
        {
            var currentStepIndex = dialogContext.ActiveDialog.State.GetValueOrAbsent(PersistedStepIndex).Map(v => (int)v).OrDefault();
            var currentState = dialogContext.ActiveDialog.State.GetValueOrAbsent(PersistedState).OrThrow(CreateStateNotFoundException);

            while (true)
            {
                if((currentStepIndex < 0) || (currentStepIndex >= dialogSteps.Length))
                {
                    return await dialogContext.EndDialogAsync(currentState, cancellationToken).ConfigureAwait(false);
                }

                var currentStep = dialogSteps[currentStepIndex];
                var flowStateTurnContext = new ImplFlowStateTurnContext<object>(dialogContext, currentState);

                var stepAction = await currentStep.Invoke(flowStateTurnContext, cancellationToken).ConfigureAwait(false);

                currentState = stepAction.State;
                dialogContext.ActiveDialog.State[PersistedState] = currentState;

                if(stepAction.ActionCode != FlowDialogActionCode.RetryTurn)
                {
                    currentStepIndex++;
                    dialogContext.ActiveDialog.State[PersistedStepIndex] = currentStepIndex;
                }

                if(stepAction.ActionCode == FlowDialogActionCode.Next)
                {
                    continue;
                }

                if(stepAction.ActionCode == FlowDialogActionCode.EndDialog)
                {
                    return await dialogContext.EndDialogAsync(currentState, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    return EndOfTurn;
                }
            }
        }

        private static InvalidOperationException CreateStateNotFoundException()
            =>
            new("A flow state was not found in the dialog state.");
    }
}