#nullable enable

using Microsoft.Bot.Schema;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;

namespace GGroupp.Internal.Timesheet.Bot
{
    public static class ReplyExtensions
    {
        public static Activity GetReplyFromCard(this Activity originalActivity, string cardName, params object[] args)
        {
            var cardTextTemplate = File.ReadAllText($"./Cards/{cardName}.json");
            var cardText = string.Format(cardTextTemplate, args);
            return originalActivity.GetReplyFromText(cardText);
        }

        public static Activity GetReplyFromCard(this Activity originalActivity, AdaptiveCardJson adaptiveCard)
        {
            var reply = new Activity
            {
                Attachments = new List<Attachment>()
            };
            reply.Attachments.Add(adaptiveCard.ToAttachement());

            originalActivity.SetReplyFields(reply);
            return reply;
        }

        private static Activity GetReplyFromText(this Activity originalActivity, string fullReplyText)
        {
            var reply = JsonConvert.DeserializeObject<Activity>(fullReplyText);
            if (reply.Attachments is null)
            {
                reply.Attachments = new List<Attachment>();
            }

            originalActivity.SetReplyFields(reply);
            return reply;
        }

        private static Attachment ToAttachement(this AdaptiveCardJson adaptiveCard)
            =>
            new()
            {
                ContentType = "application/vnd.microsoft.card.adaptive",
                Content = adaptiveCard
            };

        private static void SetReplyFields(this Activity originalActivity, IMessageActivity reply)
        {
            var tempReply = originalActivity.CreateReply(string.Empty);

            reply.ChannelId = tempReply.ChannelId;
            reply.Timestamp = tempReply.Timestamp;
            reply.From = tempReply.From;
            reply.Conversation = tempReply.Conversation;
            reply.Recipient = tempReply.Recipient;
            reply.Id = tempReply.Id;
            reply.ReplyToId = tempReply.ReplyToId;

            if (reply.Type is null)
            {
                reply.Type = ActivityTypes.Message;
            }
        }
    }
}