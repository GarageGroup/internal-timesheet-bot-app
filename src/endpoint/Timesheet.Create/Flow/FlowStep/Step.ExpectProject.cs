using System;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using GarageGroup.Infra.Telegram.Bot;

namespace GarageGroup.Internal.Timesheet;

using static TimesheetCreateResource;

partial class TimesheetCreateFlowStep
{
    internal static ChatFlow<TimesheetCreateFlowState> ExpectProjectOrSkip(
        this ChatFlow<TimesheetCreateFlowState> chatFlow, ICrmProjectApi crmProjectApi)
        =>
        chatFlow.ExpectChoiceValueOrSkip(
            crmProjectApi.CreateProjectStepOption)
        .Next(
            SetProjectTypeDisplayName);

    private static ChoiceStepOption<TimesheetCreateFlowState, TimesheetProjectState>? CreateProjectStepOption(
        this ICrmProjectApi crmProjectApi, IChatFlowContext<TimesheetCreateFlowState> context)
    {
        if (context.FlowState.Project is not null)
        {
            return null;
        }

        return new(
            choiceSetFactory: GetProjectsAsync,
            resultMessageFactory: CreateResultMessage,
            selectedItemMapper: MapProjectItem);

        ValueTask<Result<ChoiceStepSet<TimesheetProjectState>, ChatRepeatState>> GetProjectsAsync(
            ChoiceStepRequest request, CancellationToken cancellationToken)
            =>
            string.IsNullOrEmpty(request.Text) switch
            {
                true => crmProjectApi.GetLastUserProjectsAsync(context, cancellationToken),
                _ => crmProjectApi.SearchProjectsAsync(context, request, cancellationToken)
            };

        static ChatMessageSendRequest CreateResultMessage(ChoiceStepItem<TimesheetProjectState> item)
            =>
            new($"{HttpUtility.HtmlEncode(item.Value.TypeDisplayName)}: <b>{HttpUtility.HtmlEncode(item.Title)}</b>")
            {
                ReplyMarkup = new BotReplyKeyboardRemove()
            };

        TimesheetCreateFlowState MapProjectItem(ChoiceStepItem<TimesheetProjectState> item)
            =>
            context.FlowState with
            {
                Project = item.Value
            };
    }

    private static ValueTask<Result<ChoiceStepSet<TimesheetProjectState>, ChatRepeatState>> GetLastUserProjectsAsync(
        this ICrmProjectApi crmProjectApi, IChatFlowContext<TimesheetCreateFlowState> context, CancellationToken cancellationToken)
        =>
        AsyncPipeline.Pipe(
            context.FlowState, cancellationToken)
        .HandleCancellation()
        .Pipe(
            static flowState => new LastProjectSetGetIn(
                userId: flowState.UserId,
                top: MaxProjectsCount,
                minDate: GetDateUtc(-ProjectDays)))
        .PipeValue(
            crmProjectApi.GetLastAsync)
        .Map(
            success => new ChoiceStepSet<TimesheetProjectState>(
                choiceText: success.Projects.IsEmpty ? context.Localizer[SearchProjectText] : context.Localizer[ChooseOrSearchProjectText])
            {
                Items = success.Projects.Map(context.MapProjectItem)
            },
            failure => failure.ToChatRepeatState(context.Localizer[SearchProjectText]));

    private static ValueTask<Result<ChoiceStepSet<TimesheetProjectState>, ChatRepeatState>> SearchProjectsAsync(
        this ICrmProjectApi crmProjectApi,
        IChatFlowContext<TimesheetCreateFlowState> context,
        ChoiceStepRequest request,
        CancellationToken cancellationToken)
        =>
        AsyncPipeline.Pipe(
            context.FlowState, cancellationToken)
        .HandleCancellation()
        .Pipe(
            flowState => new ProjectSetSearchIn(
                searchText: request.Text,
                userId: flowState.UserId,
                top: MaxProjectsCount))
        .PipeValue(
            crmProjectApi.SearchAsync)
        .MapFailure(
            context.MapFailure)
        .MapSuccess(
            @out => new ChoiceStepSet<TimesheetProjectState>(
                choiceText: @out.Projects.IsEmpty ? context.Localizer[NotFoundProjectsText] : context.Localizer[ChooseOrSearchProjectText])
            {
                Items = @out.Projects.Map(context.MapProjectItem)
            });

    private static ChoiceStepItem<TimesheetProjectState> MapProjectItem(this IChatFlowContextBase context, ProjectSetGetItem item)
        =>
        new(
            id: item.Id.ToString(),
            title: item.Name,
            value: new()
            {
                Id = item.Id,
                Name = item.Name,
                Type = item.Type,
                TypeDisplayName = context.GetTypeDisplayName(item.Type)
            });

    private static TimesheetCreateFlowState SetProjectTypeDisplayName(IChatFlowContext<TimesheetCreateFlowState> context)
    {
        if (context.FlowState.Project is null || string.IsNullOrEmpty(context.FlowState.Project.TypeDisplayName) is false)
        {
            return context.FlowState;
        }

        return context.FlowState with
        {
            Project = context.FlowState.Project with
            {
                TypeDisplayName = context.GetTypeDisplayName(context.FlowState.Project.Type)
            }
        };
    }

    private static string GetTypeDisplayName(this IChatFlowContextBase context, TimesheetProjectType projectType)
        =>
        projectType switch
        {
            TimesheetProjectType.Opportunity => context.Localizer["Opportunity"],
            TimesheetProjectType.Lead => context.Localizer["Lead"],
            TimesheetProjectType.Incident => context.Localizer["Incident"],
            _ => context.Localizer["Project"]
        };

    private static ChatRepeatState MapFailure(this IChatFlowContextBase context, Failure<ProjectSetGetFailureCode> failure)
        =>
        failure.ToChatRepeatState(
            userMessage: failure.FailureCode switch
            {
                ProjectSetGetFailureCode.NotAllowed => context.Localizer[NotAllowedText],
                _ => throw failure.ToException()
            });
}