using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using GarageGroup.Infra.Telegram.Bot;

namespace GarageGroup.Internal.Timesheet;

using static CultureResource;

internal sealed class CultureMiddleware(IChatUserGetSupplier authorizationApi) : IChatMiddleware
{
    // The user settings (language, time zone, the disabled flag) are read from Dataverse at most once per this period
    private static readonly TimeSpan ChatUserTtl = TimeSpan.FromMinutes(5);

    private static readonly ConcurrentDictionary<long, CachedChatUser> ChatUsers = new();

    private static long? botId;

    public async ValueTask<TurnResult> InvokeAsync(IChatContext context, CancellationToken cancellationToken)
    {
        if (context.Update.MyChatMember?.NewChatMember is BotBannedChatMember)
        {
            return context.CreateTurnResult(TurnState.Complete);
        }

        var chatUser = await GetChatUserAsync(context, cancellationToken);
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
    }

    // Forgets the cached user, so the next message reads the user settings from Dataverse again
    internal static void Invalidate(long chatId)
        =>
        ChatUsers.TryRemove(chatId, out _);

    private async ValueTask<ChatUserGetOut> GetChatUserAsync(IChatContext context, CancellationToken cancellationToken)
    {
        var chatId = context.Update.Chat.Id;
        var now = DateTimeOffset.UtcNow;

        if (ChatUsers.TryGetValue(chatId, out var cached) && cached.ExpiresAt > now)
        {
            return cached.User;
        }

        // The bot identifier never changes, so it is requested from Telegram only once
        botId ??= (await context.Api.GetMeAsync(cancellationToken)).Id;

        var input = new ChatUserGetIn(botId: botId.Value, chatId: chatId);
        var result = await authorizationApi.GetChatUserAsync(input, cancellationToken);

        var chatUser = result.SuccessOrThrow(ToException);
        ChatUsers[chatId] = new(chatUser, now.Add(ChatUserTtl));

        return chatUser;

        static Failure<Unit>.Exception ToException(Failure<Unit> failure)
            =>
            failure.ToException();
    }

    private sealed record class CachedChatUser(ChatUserGetOut User, DateTimeOffset ExpiresAt);
}