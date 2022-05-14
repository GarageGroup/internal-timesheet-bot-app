using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GGroupp.Infra.Bot.Builder;
using Microsoft.Extensions.Logging;

namespace GGroupp.Internal.Timesheet;

using IFavoriteProjectSetGetFunc = IAsyncValueFunc<FavoriteProjectSetGetIn, Result<FavoriteProjectSetGetOut, Failure<FavoriteProjectSetGetFailureCode>>>;
using IProjectSetSearchFunc = IAsyncValueFunc<ProjectSetSearchIn, Result<ProjectSetSearchOut, Failure<ProjectSetSearchFailureCode>>>;

internal static class ProjectFindExtensions
{
    private const string DefaultMessage = "Нужно выбрать проект. Введите часть названия для поиска";

    private const string ChooseFavoriteMessage = "Выберите проект или введите часть названия для поиска";

    private const string ResultMessage = "Проект";

    private const int MaxProjectsCount = 6;

    internal static ValueTask<LookupValueSetOption> ShowFavorieProjects(
        this IChatFlowContext<TimesheetCreateFlowStateJson> context,
        IBotUserProvider botUserProvider,
        IFavoriteProjectSetGetFunc favoriteProjectSetGetFunc,
        CancellationToken token)
        =>
        AsyncPipeline.Pipe(
            default(Unit), token)
        .Pipe(
            botUserProvider.GetUserIdOrFailureAsync)
        .MapSuccess(
            static userId => new FavoriteProjectSetGetIn(
                userId: userId, 
                top: MaxProjectsCount))
        .ForwardValue(
            favoriteProjectSetGetFunc.InvokeAsync)
        .Fold(
            static @out => new(
                items: @out.Projects.Select(MapFavorieProjectItem).ToArray(),
                choiceText: @out.Projects.Any() ? ChooseFavoriteMessage : DefaultMessage,
                resultText: ResultMessage),
            failure => MapSearchFailure(failure, context.Logger));

    internal static ValueTask<Result<LookupValueSetOption, BotFlowFailure>> SearchProjectsAsync(
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
                choiceText: "Выберите проект",
                resultText: ResultMessage));

    private static async Task<Result<Guid, Failure<FavoriteProjectSetGetFailureCode>>> GetUserIdOrFailureAsync(
        this IBotUserProvider botUserProvider, Unit _, CancellationToken token)
    {
        var currentUser = await botUserProvider.GetCurrentUserAsync(token);
        if (currentUser is null)
        {
            return CreateFailure("Bot user must be specified");
        }

        return currentUser.Claims.GetValueOrAbsent("DataverseSystemUserId").Fold(ParseOrFailure, CreateClaimMustBeSpecifiedFailure);

        static Result<Guid, Failure<FavoriteProjectSetGetFailureCode>> ParseOrFailure(string value)
            =>
            Guid.TryParse(value, out var guid) ? guid : CreateFailure($"DataverseUserId Claim {value} is not a Guid");

        static Result<Guid, Failure<FavoriteProjectSetGetFailureCode>> CreateClaimMustBeSpecifiedFailure()
            =>
            CreateFailure("Dataverse user claim must be specified");

        static Failure<FavoriteProjectSetGetFailureCode> CreateFailure(string message)
            =>
            new(FavoriteProjectSetGetFailureCode.Unknown, message);
    }

    private static LookupValue MapFavorieProjectItem(FavoriteProjectItemGetOut item)
        =>
        new(item.Id, item.Name, item.Type.ToString("G"));

    private static LookupValue MapProjectItem(ProjectItemSearchOut item)
        =>
        new(item.Id, item.Name, item.Type.ToString("G"));

    private static LookupValueSetOption MapSearchFailure(Failure<FavoriteProjectSetGetFailureCode> failure, ILogger logger)
    {
        logger.LogError("Favorite projects failure: {failureCode} {failureMessage}", failure.FailureCode, failure.FailureMessage);
        return new(default, DefaultMessage, default, ResultMessage);
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