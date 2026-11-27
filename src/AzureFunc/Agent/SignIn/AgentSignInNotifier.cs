using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using GarageGroup.Infra.Telegram.Bot;

namespace GarageGroup.Internal.Timesheet;

internal sealed class AgentSignInNotifier(IBotApi botApi, IBotSignalHandler signalHandler) : IAgentSignInNotifier
{
    private static string? cachedBotUsername;

    public async ValueTask<string?> GetBotUsernameAsync(CancellationToken cancellationToken)
    {
        if (cachedBotUsername is not null)
        {
            return cachedBotUsername;
        }

        var result = await botApi.GetMeAsync(default, cancellationToken).ConfigureAwait(false);
        return cachedBotUsername = result.Fold(static bot => bot.Username, static _ => null);
    }

    public async ValueTask NotifySignedInAsync(AgentPendingSignIn pending, CancellationToken cancellationToken)
    {
        if (pending.SignInMessageId is int signInMessageId)
        {
            // The link is one-time, so the message with the button is useless after the sign-in. A failure is not critical
            _ = await botApi.DeleteMessageAsync(new(pending.User.TelegramChatId, signInMessageId), cancellationToken).ConfigureAwait(false);
        }

        var text = AgentSignInPage.GetLocale(pending.Locale) is "en" ? "✅ You are signed in." : "✅ Вход выполнен.";

        var result = await botApi.SendMessageAsync(new(pending.User.TelegramChatId, text), cancellationToken).ConfigureAwait(false);
        _ = result.SuccessOrThrow(static failure => failure.ToException());

        if (string.IsNullOrEmpty(pending.SourceUpdateJson))
        {
            return;
        }

        // The message that required the sign-in is sent to the bot again, so the user does not have to repeat it
        var update = JsonSerializer.Deserialize<ChatUpdate>(pending.SourceUpdateJson);
        _ = await signalHandler.HandleAsync(update, cancellationToken).ConfigureAwait(false);
    }
}
