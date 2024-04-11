using System;
using System.Threading;
using System.Threading.Tasks;
using GarageGroup.Infra.Bot.Builder;
using Microsoft.Extensions.Logging;

namespace GarageGroup.Internal.Timesheet;

partial class TimesheetCreateFlowStep
{
    internal static ChatFlow<TimesheetCreateFlowState> AwaitProject(
        this ChatFlow<TimesheetCreateFlowState> chatFlow, ICrmProjectApi crmProjectApi)
        =>
        chatFlow.AwaitLookupValue(
            crmProjectApi.GetLastProjectsAsync,
            crmProjectApi.SearchProjectsAsync,
            BuildProjectResultMessage,
            static (state, project) => state with
            {
                Project = new()
                {
                    Type = project.GetProjectType(),
                    Id = project.Id,
                    Name = project.Name
                }                
            });

    private static ValueTask<LookupValueSetOption> GetLastProjectsAsync(
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
        .Fold(
            static @out => new(
                items: @out.Projects.Map(MapProjectItem),
                choiceText: @out.Projects.IsNotEmpty ? ChooseProjectMessage : DefaultProjectMessage),
            context.LogFailure);

    private static ValueTask<Result<LookupValueSetOption, BotFlowFailure>> SearchProjectsAsync(
        this ICrmProjectApi crmProjectApi, IChatFlowContext<TimesheetCreateFlowState> context, string searchText, CancellationToken token)
        =>
        AsyncPipeline.Pipe(
            context.FlowState, token)
        .HandleCancellation()
        .Pipe(
            flowState => new ProjectSetSearchIn(
                searchText: searchText,
                userId: flowState.UserId,
                top: MaxProjectsCount))
        .PipeValue(
            crmProjectApi.SearchAsync)
        .MapFailure(
            MapToFlowFailure)
        .Filter(
            static @out => @out.Projects.IsNotEmpty,
            static _ => BotFlowFailure.From("Ничего не найдено. Попробуйте уточнить запрос"))
        .MapSuccess(
            static @out => new LookupValueSetOption(
                items: @out.Projects.Map(MapProjectItem),
                choiceText: "Выберите проект"));

    private static string BuildProjectResultMessage(IChatFlowContext<TimesheetCreateFlowState> context, LookupValue projectValue)
        =>
        $"{projectValue.GetProjectType().ToStringRussianCulture()}: {context.CreateBoldText(projectValue.Name)}";

    private static LookupValue MapProjectItem(ProjectSetGetItem item)
        =>
        new(item.Id, item.Name, item.Type.ToString("G"));

    private static TimesheetProjectType GetProjectType(this LookupValue projectValue)
        =>
        Enum.Parse<TimesheetProjectType>(projectValue.Data.OrEmpty());

    private static LookupValueSetOption LogFailure(this ILoggerSupplier context, Failure<ProjectSetGetFailureCode> failure)
    {
        context.Logger.LogError(
            failure.SourceException,
            "GetLastProjects failure: {failureCode} {failureMessage}",
            failure.FailureCode,
            failure.FailureMessage);

        return new(default, DefaultProjectMessage, default);
    }

    private static BotFlowFailure MapToFlowFailure(Failure<ProjectSetGetFailureCode> failure)
        =>
        (failure.FailureCode switch
        {
            ProjectSetGetFailureCode.NotAllowed
                => "При поиске проектов произошла ошибка. У вашей учетной записи не достаточно разрешений. Обратитесь к администратору приложения",
            ProjectSetGetFailureCode.TooManyRequests
                => "Слишком много обращений к сервису. Попробуйте повторить попытку через несколько секунд",
            _
                => "При поиске проектов произошла непредвиденная ошибка. Обратитесь к администратору или повторите попытку позднее"
        })
        .Pipe(
            message => BotFlowFailure.From(message, failure.FailureMessage));
}