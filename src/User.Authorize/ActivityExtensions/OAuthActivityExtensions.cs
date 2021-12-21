using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Schema;

namespace GGroupp.Internal.Timesheet;

internal static partial class OAuthActivityExtensions
{
    private static readonly IReadOnlyCollection<string> notSupportedOAuthCardChannles;

    private static readonly Regex magicCodeRegex;

    static OAuthActivityExtensions()
    {
        notSupportedOAuthCardChannles = new[] { Channels.Cortana, Channels.Skype, Channels.Skypeforbusiness };
        magicCodeRegex = new(@"(\d{6})");
    }

    private static bool IsOAuthCardSupported(this IActivity activity)
        =>
        notSupportedOAuthCardChannles.Contains(activity.ChannelId, StringComparer.InvariantCultureIgnoreCase) is false;

    private static bool IsEmulator(this IActivity activity)
        =>
        activity.ChannelId.EqualsInvariant(Channels.Emulator);

    private static bool IsTokenResponseEvent(this Activity activity)
        =>
        activity.Type.EqualsInvariant(ActivityTypes.Event) && activity.Name.EqualsInvariant(SignInConstants.TokenResponseEventName);

    private static bool IsTeamsVerificationInvoke(this Activity activity)
        =>
        activity.Type.EqualsInvariant(ActivityTypes.Invoke) && activity.Name.EqualsInvariant(SignInConstants.VerifyStateOperationName);

    private static bool IsTokenExchangeRequestInvoke(this Activity activity)
        =>
        activity.Type.EqualsInvariant(ActivityTypes.Invoke) && activity.Name.EqualsInvariant(SignInConstants.TokenExchangeOperationName);

    private static IActivity ToActivity(this Attachment attachment, string inputHint)
        =>
        MessageFactory.Attachment(attachment, text: default, ssml: default, inputHint: inputHint);

    private static bool EqualsInvariant(this string? a, string? b)
        =>
        string.Equals(a, b, StringComparison.CurrentCultureIgnoreCase);
}