#nullable enable

using Microsoft.Bot.Schema;

namespace GGroupp.Internal.Timesheet.Bot
{
    public readonly struct FlowDialogFailure
    {
        public static readonly FlowDialogFailure EndDialog = new(FlowDialogFailureCode.EndDialog, null);

        public static readonly FlowDialogFailure RetryTurn = new(FlowDialogFailureCode.RetryTurn, null);

        public FlowDialogFailure(FlowDialogFailureCode failureCode, IActivity? failureActivity)
        {
            FailureCode = failureCode;
            FailureActivity = failureActivity;
        }

        public IActivity? FailureActivity { get; }

        public FlowDialogFailureCode FailureCode { get; }
    }
}