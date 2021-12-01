using System;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Schema;

namespace GGroupp.Infra.Bot.Builder;

partial class ActivityExtensions
{
    public static bool IsCardSupported(this Activity activity)
        =>
        InnerIsCardSupported(
            activity ?? throw new ArgumentNullException(nameof(activity)));

    private static bool InnerIsCardSupported(this Activity activity)
        =>
        activity.ChannelId switch
        {
            Channels.Msteams or Channels.Webchat or Channels.Emulator => true,
            _ => false
        };
}

