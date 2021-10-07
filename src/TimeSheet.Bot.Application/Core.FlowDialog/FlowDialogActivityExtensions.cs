#nullable enable

using System.Diagnostics.CodeAnalysis;
using Microsoft.Bot.Schema;

namespace GGroupp.Internal.Timesheet.Bot
{
    public static class FlowDialogActivityExtensions
    {
        public static FlowDialogFailure ToEndDialogFailure([AllowNull] this IActivity activity)
            =>
            new(FlowDialogFailureCode.EndDialog, activity);

        public static FlowDialogFailure ToRetryTurnFailure([AllowNull] this IActivity activity)
            =>
            new(FlowDialogFailureCode.RetryTurn, activity);
    }
}