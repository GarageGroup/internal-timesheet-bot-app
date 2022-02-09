using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GGroupp.Infra.Bot.Builder;

namespace GGroupp.Internal.Timesheet;

using IProjectSetSearchFunc = IAsyncValueFunc<ProjectSetSearchIn, Result<ProjectSetSearchOut, Failure<ProjectSetSearchFailureCode>>>;

internal static class ProjectFindFlowStep
{
    private const int MaxProjectsCount = 5;

    internal static ChatFlow<TimesheetCreateFlowStateJson> FindProject(
        this ChatFlow<TimesheetCreateFlowStateJson> chatFlow,
        IProjectSetSearchFunc projectSetSearchFunc)
        =>
        chatFlow.SendText(
            static _ => "Нужно выбрать проект. Введите часть названия для поиска")
        .AwaitLookupValue(
            (_, search, token) => projectSetSearchFunc.SearchProjectsAsync(search, token),
            static (flowState, projectValue) => flowState with
            {
                ProjectType = Enum.Parse<TimesheetProjectType>(projectValue.Data.OrEmpty()),
                ProjectId = projectValue.Id,
                ProjectName = projectValue.Name
            });

    private static ValueTask<Result<LookupValueSetOption, BotFlowFailure>> SearchProjectsAsync(
        this IProjectSetSearchFunc projectSetSearchFunc, string seachText, CancellationToken cancellationToken)
        =>
        AsyncPipeline.Pipe(
            seachText, cancellationToken)
        .Pipe(
            static text => new ProjectSetSearchIn(
                searchText: text,
                top: MaxProjectsCount))
        .PipeValue(
            projectSetSearchFunc.InvokeAsync)
        .MapFailure(
            MapToFlowFailure)
        .Filter(
            static @out => @out.Projects.Any(),
            static _ => BotFlowFailure.From("Ничего не найдено. Попробуйте уточнить запрос"))
        .MapSuccess(
            static @out => new LookupValueSetOption(
                items: @out.Projects.Select(MapProjectItem).ToArray(),
                choiceText: "Выберите проект"));

    private static LookupValue MapProjectItem(ProjectItemSearchOut item)
        =>
        new(item.Id, item.Name, item.Type.ToString("G"));

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