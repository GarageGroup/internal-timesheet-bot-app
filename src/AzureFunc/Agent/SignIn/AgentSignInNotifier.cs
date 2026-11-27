using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using GarageGroup.Infra.Telegram.Bot;

namespace GarageGroup.Internal.Timesheet;

internal sealed class AgentSignInNotifier(IBotApi botApi, IBotSignalHandler signalHandler) : IAgentSignInNotifier
{
    public async ValueTask NotifySignedInAsync(AgentPendingSignIn pending, CancellationToken cancellationToken)
    {
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
