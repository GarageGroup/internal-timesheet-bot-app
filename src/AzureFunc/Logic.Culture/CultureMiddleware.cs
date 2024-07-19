using System;
using System.Threading;
using System.Threading.Tasks;
using GarageGroup.Infra.Telegram.Bot;

namespace GarageGroup.Internal.Timesheet;

using static CultureResource;

internal sealed class CultureMiddleware(IChatUserGetSupplier authorizationApi) : IChatMiddleware
{
    public async ValueTask<TurnResult> InvokeAsync(IChatContext context, CancellationToken cancellationToken)
    {
        if (context.Update.MyChatMember?.NewChatMember is BotBannedChatMember)
        {
            return context.CreateTurnResult(TurnState.Complete);
        }

        var bot = await context.Api.GetMeAsync(cancellationToken);

        var input = new ChatUserGetIn(botId: bot.Id, chatId: context.Update.Chat.Id);
        var result = await authorizationApi.GetChatUserAsync(input, cancellationToken);

        var chatUser = result.SuccessOrThrow(ToException);
        if (chatUser.User is not null)
        {
            context = ((IChatContextWithUserSupplier)context).WithUser(chatUser.User);
        }

        var localizer = context.GetLocalizer(BaseName);
        if (chatUser.IsDisabled)
        {
            var disabledText = localizer[UserDisabledMessageName];
            _ = await context.Api.SendHtmlModeTextAndRemoveReplyKeyboardAsync(disabledText, cancellationToken);

            return context.CreateTurnResult(TurnState.Cancelled);
        }

        return context.CreateTurnResult(TurnState.Complete);

        static Failure<Unit>.Exception ToException(Failure<Unit> failure)
            =>
            failure.ToException();
    }
}