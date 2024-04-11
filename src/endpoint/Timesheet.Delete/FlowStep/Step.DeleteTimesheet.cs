using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using GarageGroup.Infra.Bot.Builder;
using Microsoft.Bot.Builder;
using Microsoft.Extensions.Logging;

namespace GarageGroup.Internal.Timesheet;

partial class TimesheetDeleteFlowStep
{
    internal static ChatFlow<TimesheetDeleteFlowState> DeleteTimesheet(
        this ChatFlow<TimesheetDeleteFlowState> chatFlow, ICrmTimesheetApi timesheetApi)
        =>
        chatFlow.ForwardValue(timesheetApi.Delete);

    private static ValueTask<ChatFlowJump<TimesheetDeleteFlowState>> Delete(
        this ICrmTimesheetApi crmTimesheetApi, 
        IChatFlowContext<TimesheetDeleteFlowState> context, 
        CancellationToken cancellationToken)
        =>
        AsyncPipeline.Pipe(
            context, cancellationToken)
        .Pipe(
            state => (state, crmTimesheetApi))
        .PipeValue(
            DeleteTimesheetAsync)
        .OnFailure(
            context.Logger.LogDeleteFailures)
        .Map(
            _ => context.FlowState,
            ToBreakState)
        .Fold(
            ChatFlowJump.Next,
            ChatFlowJump.Break<TimesheetDeleteFlowState>);

    private static void LogDeleteFailures (this ILogger logger, DeleteFailure deleteFailure)
    {
        foreach (var failure in deleteFailure.Failures)
        {
            logger.LogError(failure.Failure.SourceException, 
                "Не удалось удалить timesheet. TimesheetId: {0}. FailureMessage: {1}. FailureCode: {2}", 
                failure.Id, failure.Failure.FailureMessage, failure.Failure.FailureCode);
        }
    }

    private static async ValueTask<Result<Unit, DeleteFailure>> DeleteTimesheetAsync(
        (IChatFlowContext<TimesheetDeleteFlowState> Context, ICrmTimesheetApi CrmTimesheetApi) input, 
        CancellationToken cancellationToken)
    {
        var turnContext = (ITurnContext)input.Context;
        await turnContext.DeleteActivityAsync(turnContext.Activity.Id, cancellationToken);

        var failures = new ConcurrentBag<(Failure<TimesheetDeleteFailureCode> Failure, Guid Id)>();
        await Parallel.ForEachAsync(input.Context.FlowState.DeleteTimesheetsId.AsEnumerable(), cancellationToken, InnerDeleteAsync);

        if (failures.IsEmpty)
        {
            return default(Unit);
        }

        return new DeleteFailure(failures.ToFlatArray());

        async ValueTask InnerDeleteAsync(Guid id, CancellationToken token)
        {
            var timesheetDeleteIn = new TimesheetDeleteIn(id);
            var result = await input.CrmTimesheetApi.DeleteAsync(timesheetDeleteIn, token);
            
            if (result.IsFailure)
            {
                failures.Add((result.FailureOrThrow(), id));
            }
        }
    }

    private static ChatFlowBreakState ToBreakState(DeleteFailure failures)
        =>
        ("Не удалось удалить одну или несколько записей")
        .Pipe(
            ChatFlowBreakState.From);

    internal readonly record struct DeleteFailure(FlatArray<(Failure<TimesheetDeleteFailureCode> Failure, Guid Id)> Failures);
}