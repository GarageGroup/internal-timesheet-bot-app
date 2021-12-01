using System;
using Microsoft.Bot.Schema;

namespace GGroupp.Infra.Bot.Builder;

partial class ActivityExtensions
{
    public static bool IsMessageType(this Activity activity)
        =>
        InnerIsMessageType(
            activity ?? throw new ArgumentNullException(nameof(activity)));

    private static bool InnerIsMessageType(this Activity activity)
        =>
        activity.Type is ActivityTypes.Message;
}

