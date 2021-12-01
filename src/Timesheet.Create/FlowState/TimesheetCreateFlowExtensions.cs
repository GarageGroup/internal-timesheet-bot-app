using System;
using Microsoft.Extensions.Logging;

namespace GGroupp.Internal.Timesheet;

internal static class TimesheetCreateFlowExtensions
{
    internal static Unit LogFailure<TFailureCode>(this ILogger logger, Failure<TFailureCode> failure)
        where TFailureCode : struct
        =>
        Unit.Invoke(
            () => logger.LogError(failure.FailureMessage));
}