using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GGroupp.Infra.Bot.Builder;
using Microsoft.Extensions.Logging;

namespace GGroupp.Internal.Timesheet;

using IFavoriteSetGetFunc = IAsyncValueFunc<FavoriteProjectSetGetIn, Result<FavoriteProjectSetGetOut, Failure<FavoriteProjectSetGetFailureCode>>>;
using IProjectSetSearchFunc = IAsyncValueFunc<ProjectSetSearchIn, Result<ProjectSetSearchOut, Failure<ProjectSetSearchFailureCode>>>;

internal static class ProjectFindExtensions
{
    private const string DefaultMessage = "Нужно выбрать проект. Введите часть названия для поиска";

    private const string ChooseFavoriteMessage = "Выберите проект или введите часть названия для поиска";

    private const int MaxProjectsCount = 6;

    internal static ValueTask<LookupValueSetOption> GetFavoriteOptionsAsync(
        this IFavoriteSetGetFunc favoriteProjectSetGetFunc,
        IChatFlowContext<TimesheetCreateFlowState> context,
        CancellationToken token)
        =>
        AsyncPipeline.Pipe(
            context.FlowState.UserId, token)
        .HandleCancellation()
        .Pipe(
            static userId => new FavoriteProjectSetGetIn(
                userId: userId, 
                top: MaxProjectsCount))
        .PipeValue(
            favoriteProjectSetGetFunc.InvokeAsync)
        .Fold(
            static @out => new(
                items: @out.Projects.Select(MapFavorieProjectItem).ToArray(),
                choiceText: @out.Projects.Any() ? ChooseFavoriteMessage : DefaultMessage),
            failure => MapSearchFailure(failure, context.Logger));

    internal static ValueTask<Result<LookupValueSetOption, BotFlowFailure>> SearchProjectsAsync(
        this IProjectSetSearchFunc projectSetSearchFunc, string seachText, CancellationToken cancellationToken)
        =>
        AsyncPipeline.Pipe(
            seachText, cancellationToken)
        .HandleCancellation()
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
    private static LookupValue MapFavorieProjectItem(FavoriteProjectItemGetOut item)
        =>
        new(item.Id, item.Name, item.Type.ToString("G"));

    private static LookupValue MapProjectItem(ProjectItemSearchOut item)
        =>
        new(item.Id, item.Name, item.Type.ToString("G"));

    private static LookupValueSetOption MapSearchFailure(Failure<FavoriteProjectSetGetFailureCode> failure, ILogger logger)
    {
        logger.LogError("Favorite projects failure: {failureCode} {failureMessage}", failure.FailureCode, failure.FailureMessage);
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