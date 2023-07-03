using System;
using System.Threading;
using System.Threading.Tasks;
using GarageGroup.Infra.Bot.Builder;
using Microsoft.Extensions.Logging;

namespace GarageGroup.Internal.Timesheet;

internal static class ProjectAwaitHelper
{
    private const string DefaultMessage = "Нужно выбрать проект. Введите часть названия для поиска";

    private const string ChooseFavoriteMessage = "Выберите проект или введите часть названия для поиска";

    private const int MaxProjectsCount = 6;

    internal static ValueTask<LookupValueSetOption> GetFavoriteProjectsAsync(
        this IFavoriteProjectSetGetSupplier timesheetApi, IChatFlowContext<TimesheetCreateFlowState> context, CancellationToken cancellationToken)
        =>
        AsyncPipeline.Pipe(
            context.FlowState, cancellationToken)
        .HandleCancellation()
        .Pipe(
            static flowState => new FavoriteProjectSetGetIn
            {
                UserId = flowState.UserId,
                Top = MaxProjectsCount,
                CallerUserId = flowState.UserId
            })
        .PipeValue(
            timesheetApi.GetFavoriteProjectSetAsync)
        .Fold(
            static @out => new(
                items: @out.Projects.Map(MapFavorieProjectItem),
                choiceText: @out.Projects.IsNotEmpty ? ChooseFavoriteMessage : DefaultMessage),
            context.LogFailure);

    internal static ValueTask<Result<LookupValueSetOption, BotFlowFailure>> SearchProjectsAsync(
        this IProjectSetSearchSupplier timesheetApi, IChatFlowContext<TimesheetCreateFlowState> context, string seachText, CancellationToken token)
        =>
        AsyncPipeline.Pipe(
            context.FlowState, token)
        .HandleCancellation()
        .Pipe(
            flowState => new ProjectSetSearchIn(seachText)
            {
                Top = MaxProjectsCount,
                CallerUserId = flowState.UserId
            })
        .PipeValue(
            timesheetApi.SearchProjectSetAsync)
        .MapFailure(
            MapToFlowFailure)
        .Filter(
            static @out => @out.Projects.IsNotEmpty,
            static _ => BotFlowFailure.From("Ничего не найдено. Попробуйте уточнить запрос"))
        .MapSuccess(
            static @out => new LookupValueSetOption(
                items: @out.Projects.Map(MapProjectItem),
                choiceText: "Выберите проект"));

    internal static string CreateResultMessage(IChatFlowContext<TimesheetCreateFlowState> context, LookupValue projectValue)
        =>
        $"{projectValue.GetProjectType().ToStringRussianCulture()}: {context.CreateBoldText(projectValue.Name)}";

    internal static TimesheetCreateFlowState WithProjectValue(TimesheetCreateFlowState flowState, LookupValue projectValue)
        =>
        flowState with
        {
            ProjectType = projectValue.GetProjectType(),
            ProjectId = projectValue.Id,
            ProjectName = projectValue.Name
        };

    private static LookupValue MapFavorieProjectItem(FavoriteProjectItem item)
        =>
        new(item.Id, item.Name, item.Type.ToString("G"));

    private static LookupValue MapProjectItem(ProjectSearchItem item)
        =>
        new(item.Id, item.Name, item.Type.ToString("G"));

    private static TimesheetProjectType GetProjectType(this LookupValue projectValue)
        =>
        Enum.Parse<TimesheetProjectType>(projectValue.Data.OrEmpty());

    private static LookupValueSetOption LogFailure(this ILoggerSupplier context, Failure<FavoriteProjectSetGetFailureCode> failure)
    {
        context.Logger.LogError("Favorite projects failure: {failureCode} {failureMessage}", failure.FailureCode, failure.FailureMessage);
        return new(default, DefaultMessage, default);
    }

    private static BotFlowFailure MapToFlowFailure(Failure<ProjectSetSearchFailureCode> failure)
        =>
        (failure.FailureCode switch
        {
            ProjectSetSearchFailureCode.NotAllowed
                => "При поиске проектов произошла ошибка. У вашей учетной записи не достаточно разрешений. Обратитесь к администратору приложения",
            ProjectSetSearchFailureCode.TooManyRequests
                => "Слишком много обращений к сервису. Попробуйте повторить попытку через несколько секунд",
            _
                => "При поиске проектов произошла непредвиденная ошибка. Обратитесь к администратору или повторите попытку позднее"
        })
        .Pipe(
            message => BotFlowFailure.From(message, failure.FailureMessage));
}