using System;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;

namespace GGroupp.Infra.Bot.Builder;

partial class ActivityExtensions
{
    public static IMessageActivity ToActivity(this Attachment attachment)
        =>
        MessageFactory.Attachment(
            attachment ?? throw new ArgumentNullException(nameof(attachment)));
}

