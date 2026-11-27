using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace GarageGroup.Internal.Timesheet;

internal sealed record class AgentSignInCallbackResponse(HttpStatusCode StatusCode, string Html);

// Notifies the Telegram user and continues the request that was interrupted by the sign-in
internal interface IAgentSignInNotifier
{
    ValueTask NotifySignedInAsync(AgentPendingSignIn pending, CancellationToken cancellationToken);
}

// Handles the form posted by Microsoft Entra ID. The first request shows a confirmation page that names the Telegram account,
// the second one (with the confirm field) completes the sign-in. The confirmation protects a user who was given someone else's link
internal sealed class AgentSignInCallbackHandler(IAgentSignInService signInService, IAgentSignInNotifier notifier, ILogger logger)
{
    internal const string ConfirmField = "confirm";

    public async ValueTask<AgentSignInCallbackResponse> HandleAsync(
        IReadOnlyDictionary<string, string> form, CancellationToken cancellationToken)
    {
        var state = form.GetValueOrDefault("state").OrEmpty();
        var code = form.GetValueOrDefault("code").OrEmpty();

        if (string.IsNullOrEmpty(form.GetValueOrDefault("error")) is false)
        {
            logger.LogWarning(
                "The sign-in has failed: {error}. {description}", form.GetValueOrDefault("error"), form.GetValueOrDefault("error_description"));

            return CreateFailure(AgentSignInPage.GetLocale(null));
        }

        if (string.IsNullOrEmpty(state) || string.IsNullOrEmpty(code))
        {
            return CreateFailure(AgentSignInPage.GetLocale(null), HttpStatusCode.BadRequest);
        }

        var pending = await signInService.FindPendingAsync(state, cancellationToken).ConfigureAwait(false);
        if (pending is null)
        {
            return CreateFailure(AgentSignInPage.GetLocale(null));
        }

        if (form.ContainsKey(ConfirmField) is false)
        {
            return new(HttpStatusCode.OK, AgentSignInPage.BuildConfirmation(pending, code));
        }

        var completed = await signInService.CompleteAsync(state, code, cancellationToken).ConfigureAwait(false);
        if (completed is null)
        {
            return CreateFailure(pending.Locale);
        }

        try
        {
            await notifier.NotifySignedInAsync(completed, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(exception, "Failed to notify the Telegram user {userId} about the sign-in", completed.User.TelegramUserId);
        }

        return new(HttpStatusCode.OK, AgentSignInPage.BuildSuccess(completed.Locale));
    }

    private static AgentSignInCallbackResponse CreateFailure(string locale, HttpStatusCode statusCode = HttpStatusCode.OK)
        =>
        new(statusCode, AgentSignInPage.BuildFailure(locale));
}
