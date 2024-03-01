using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using GarageGroup.Infra.Bot.Builder;
using Microsoft.Extensions.Logging;

namespace GarageGroup.Internal.Timesheet;

partial class TimesheetDeleteFlowStep
{
    internal static ChatFlow<DeleteTimesheetFlowState> DeleteTimesheet(
        this ChatFlow<DeleteTimesheetFlowState> chatFlow, ICrmTimesheetApi timesheetApi)
        =>
        chatFlow.ForwardValue(timesheetApi.Delete);

    private static ValueTask<ChatFlowJump<DeleteTimesheetFlowState>> Delete(
        this ICrmTimesheetApi crmTimesheetApi, IChatFlowContext<DeleteTimesheetFlowState> context, CancellationToken cancellationToken)
        =>
        AsyncPipeline.Pipe(
            context.FlowState, cancellationToken)
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
            ChatFlowJump.Break<DeleteTimesheetFlowState>);

    private static void LogDeleteFailures (this ILogger logger, DeleteFailure deleteFailure)
    {
        foreach (var failure in deleteFailure.Failures)
        {
            logger.LogError(failure.Failure.SourceException, 
                "Не удалось удалить timesheet. TimesheetId: {0}. FailureMessage: {1}. FailureCode: {2}"
                , failure.Id, failure.Failure.FailureMessage, failure.Failure.FailureCode);
        }
    }

    private static async ValueTask<Result<Unit, DeleteFailure>> DeleteTimesheetAsync(
        (DeleteTimesheetFlowState State, ICrmTimesheetApi CrmTimesheetApi) input, CancellationToken cancellationToken)
    {
        var failures = new ConcurrentBag<(Failure<TimesheetDeleteFailureCode> Failure, Guid Id)>();
        await Parallel.ForEachAsync(input.State.DeleteTimesheetsId.AsEnumerable(), cancellationToken, InnerDeleteAsync);

        if (failures.IsEmpty)
        {
            return default(Unit);
        }

        return new DeleteFailure(failures.ToFlatArray());

        async ValueTask InnerDeleteAsync(Guid id, CancellationToken cancellationToken)
        {
            var timesheetDeleteIn = new TimesheetDeleteIn(id);
            var result = await input.CrmTimesheetApi.DeleteAsync(timesheetDeleteIn, cancellationToken);
            
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
            message => ChatFlowBreakState.From(message));

    internal readonly record struct DeleteFailure(FlatArray<(Failure<TimesheetDeleteFailureCode> Failure, Guid Id)> Failures);
}