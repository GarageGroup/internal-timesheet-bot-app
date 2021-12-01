using System;
using System.Threading;
using System.Threading.Tasks;
using GGroupp.Infra.Bot.Builder;
using Microsoft.Extensions.Logging;

namespace GGroupp.Internal.Timesheet;

using ITimesheetCreateFunc = IAsyncValueFunc<TimesheetCreateIn, Result<TimesheetCreateOut, Failure<TimesheetCreateFailureCode>>>;

internal static class TimesheetCreateFlowStep
{
    private const string SuccessMessage
        =
        "Списание времени создано успешно";

    private const string FailureMessage
        =
        "При создании списания времени произошла непредвиденная ошибка. Обратитесь к администратору или повторите попытку позднее";

    internal static ChatFlow<Unit> CreateTimesheet(
        this ChatFlow<TimesheetCreateFlowStateJson> chatFlow,
        ITimesheetCreateFunc timesheetCreateFunc,
        ILogger logger)
        =>
        chatFlow.ForwardValue(
            timesheetCreateFunc.CreateTimesheetAsync)
        .MapFlowState(
            result =>
            {
                if (result.IsSuccess)
                {
                    if (string.IsNullOrEmpty(result.Message) is false)
                    {
                        logger.LogInformation(result.Message);
                    }

                    return SuccessMessage;
                }
                else
                {
                    if (string.IsNullOrEmpty(result.Message) is false)
                    {
                        logger.LogError(result.Message);
                    }

                    return FailureMessage;
                }
            })
        .SendText(
            context => context.FlowState)
        .MapFlowState(
            Unit.From);

    private static ValueTask<ChatFlowAction<TimesheetCreateFlowResultJson>> CreateTimesheetAsync(
        this ITimesheetCreateFunc timesheetCreateFunc,
        IChatFlowContext<TimesheetCreateFlowStateJson> context,
        CancellationToken cancellationToken)
        =>
        AsyncPipeline.Pipe(
            context.FlowState, cancellationToken)
        .Pipe(
            state => new TimesheetCreateIn(
                ownerId: default,
                date: state.Date,
                projectId: state.ProjectId,
                projectType: MapProjectType(state.ProjectType),
                duration: state.ValueHours,
                description: state.Description))
        .PipeValue(
            timesheetCreateFunc.InvokeAsync)
        .Fold(
            @out => new TimesheetCreateFlowResultJson
            {
                IsSuccess = true,
                Message = $"Timesheet {@out.TimesheetId} has been successfully created"
            },
            failure => new TimesheetCreateFlowResultJson
            {
                IsSuccess = false,
                Message = failure.FailureMessage
            })
        .Pipe(
            ChatFlowAction.Next);

    private static TimesheetProjectType MapProjectType(TimesheetCreateFlowProjectType projectType)
        =>
        projectType switch
        {
            TimesheetCreateFlowProjectType.Lead => TimesheetProjectType.Lead,
            TimesheetCreateFlowProjectType.Opportunity => TimesheetProjectType.Opportunity,
            _ => TimesheetProjectType.Project
        };
}