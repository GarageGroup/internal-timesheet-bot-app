using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using GarageGroup.Infra.Telegram.Bot;
using Microsoft.Extensions.Logging;

namespace GarageGroup.Internal.Timesheet;

internal static class AgentChatContextExtensions
{
    // Telegram shows the typing status for about five seconds, so it is repeated while the agent works
    private static readonly TimeSpan TypingInterval = TimeSpan.FromSeconds(4);

    // Returns 'ru' or 'en'. The language comes from the user settings in Dataverse;
    // the Telegram client language is used only for a user who is not found there
    internal static string GetAgentLocale(this IChatContext context, string? telegramLanguageCode)
    {
        var culture = context.User.Culture;
        var language = ReferenceEquals(culture, CultureInfo.InvariantCulture) || string.IsNullOrEmpty(culture.Name)
            ? telegramLanguageCode
            : culture.TwoLetterISOLanguageName;

        return language?.StartsWith("ru", StringComparison.OrdinalIgnoreCase) is true ? "ru" : "en";
    }

    // Keeps the typing status in the chat until the operation completes
    internal static async Task<T> WithTypingAsync<T>(
        this IChatContext context, Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken)
    {
        using var typingCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var typingTask = SendTypingAsync(context, typingCancellation.Token);

        try
        {
            return await operation.Invoke(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            await typingCancellation.CancelAsync().ConfigureAwait(false);
            await typingTask.ConfigureAwait(false);
        }
    }

    private static async Task SendTypingAsync(IChatContext context, CancellationToken cancellationToken)
    {
        try
        {
            while (cancellationToken.IsCancellationRequested is false)
            {
                await context.Api.SendChatActionAsync(BotChatAction.Typing, cancellationToken).ConfigureAwait(false);
                await Task.Delay(TypingInterval, cancellationToken).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
            // The operation has completed
        }
        catch (Exception exception)
        {
            // The typing status is cosmetic: its failure must not break the answer
            context.GetLogger<AgentChatReply>().LogWarning(exception, "Failed to send the typing chat action");
        }
    }
}
