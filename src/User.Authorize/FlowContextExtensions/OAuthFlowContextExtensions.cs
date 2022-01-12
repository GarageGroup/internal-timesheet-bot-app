using System;
using Microsoft.Bot.Builder;
using Microsoft.Extensions.Logging;

namespace GGroupp.Internal.Timesheet;

internal static partial class OAuthFlowContextExtensions
{
    private const string UserNotFoundFailureMessage
        =
        "Пользователь не найден. Возомжно у вас нет прав для доступа в систему";

    private const string UnsuccessfulTokenFailureMessage
        =
        "Не удалось авторизоваться. Повторите попытку";

    private const string EnterButtonTitle = "Вход";

    private const string EnterText = "Войдите в свою учетную запись";

    private static Result<IExtendedUserTokenProvider, FlowFailure> GetUserTokenPrividerOrFailure(
        this IOAuthFlowContext context)
    {
        if (context.Adapter is IExtendedUserTokenProvider adapter)
        {
            return Result.Success(adapter);
        }

        context.GetLogger().LogError("UserTokenClient must be specified in the turn state");
        return new FlowFailure(FlowFailure.UnexpectedFailureMessage);
    }
}