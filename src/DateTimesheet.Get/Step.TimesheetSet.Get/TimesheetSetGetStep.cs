using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GGroupp.Infra.Bot.Builder;

namespace GGroupp.Internal.Timesheet;

using ITimesheetSetGetFunc = IAsyncValueFunc<TimesheetSetGetIn, Result<TimesheetSetGetOut, Failure<TimesheetSetGetFailureCode>>>;

internal static class TimesheetSetGetStep
{
    internal static ChatFlow<Unit> GetTimesheet(
        this ChatFlow<DateTimesheetFlowState> chatFlow,
        IBotUserProvider botUserProvider,
        ITimesheetSetGetFunc timesheetSetGetFunc)
        =>
        chatFlow.ForwardValue(
            (context, token) => context.GetTimesheetSetAsync(botUserProvider, timesheetSetGetFunc, token))
        .SendActivity(
            TimesheetSetGetActivity.CreateActivity)
        .MapFlowState(
            Unit.From);
    private static ValueTask<ChatFlowJump<DateTimesheetFlowState>> GetTimesheetSetAsync(
        this IChatFlowContext<DateTimesheetFlowState> context,
        IBotUserProvider botUserProvider,
        ITimesheetSetGetFunc timesheetSetGetFunc,
        CancellationToken cancellationToken)
        =>
        AsyncPipeline.Pipe(
            default(Unit), cancellationToken)
        .Pipe(
            botUserProvider.GetUserIdOrFailureAsync)
        .MapSuccess(
            userId => new TimesheetSetGetIn(
                userId: userId,
                date: context.FlowState.Date))
        .ForwardValue(
            timesheetSetGetFunc.InvokeAsync)
        .MapFailure(
            ToBreakState)
        .MapSuccess(
            @out => context.FlowState with
            {
                Timesheets = @out.Timesheets.Select(MapTimesheet).ToArray()
            })
        .Fold(
            ChatFlowJump.Next,
            ChatFlowJump.Break<DateTimesheetFlowState>);

    private static async Task<Result<Guid, Failure<TimesheetSetGetFailureCode>>> GetUserIdOrFailureAsync(
        this IBotUserProvider botUserProvider, Unit _, CancellationToken token)
    {
        var currentUser = await botUserProvider.GetCurrentUserAsync(token);
        if (currentUser is null)
        {
            return CreateFailure("Bot user must be specified");
        }

        return currentUser.Claims.GetValueOrAbsent("DataverseSystemUserId").Fold(ParseOrFailure, CreateClaimMustBeSpecifiedFailure);

        static Result<Guid, Failure<TimesheetSetGetFailureCode>> ParseOrFailure(string value)
            =>
            Guid.TryParse(value, out var guid) ? guid : CreateFailure($"DataverseUserId Claim {value} is not a Guid");

        static Result<Guid, Failure<TimesheetSetGetFailureCode>> CreateClaimMustBeSpecifiedFailure()
            =>
            CreateFailure("Dataverse user claim must be specified");

        static Failure<TimesheetSetGetFailureCode> CreateFailure(string message)
            =>
            new(TimesheetSetGetFailureCode.Unknown, message);
    }

    private static TimesheetJson MapTimesheet(TimesheetSetItemGetOut timesheet)
        =>
        new()
        {
            Duration = timesheet.Duration,
            ProjectName = timesheet.ProjectName,
            Description = timesheet.Description
        };

    private static ChatFlowBreakState ToBreakState(Failure<TimesheetSetGetFailureCode> failure)
        =>
        (failure.FailureCode switch
        {
            TimesheetSetGetFailureCode.NotAllowed
                => "Данная операция не разрешена для вашей учетной записи. Обратитесь к администратору",
            _
                => "Произошла непредвиденная ошибка. Обратитесь к администратору или повторите попытку позднее"
        })
        .Pipe(
            message => ChatFlowBreakState.From(message, failure.FailureMessage));
}