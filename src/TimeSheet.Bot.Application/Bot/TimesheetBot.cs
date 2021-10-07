#nullable enable

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;

namespace GGroupp.Internal.Timesheet.Bot
{
    internal sealed class TimesheetBot : ActivityHandler
    {
        public static TimesheetBot Create(ConversationState conversationState, UserState userState, Dialog dialog)
            =>
            new(
                conversationState ?? throw new ArgumentNullException(nameof(conversationState)),
                userState ?? throw new ArgumentNullException(nameof(userState)),
                dialog ?? throw new ArgumentNullException(nameof(dialog)));

        private readonly Dialog dialog;

        private readonly BotState conversationState;

        private readonly BotState userState;

        private TimesheetBot(ConversationState conversationState, UserState userState, Dialog dialog)
        {
            this.conversationState = conversationState;
            this.userState = userState;
            this.dialog = dialog;
        }

        public override async Task OnTurnAsync(
            ITurnContext turnContext, CancellationToken cancellationToken = default)
        {
            await base.OnTurnAsync(turnContext, cancellationToken);

            // Save any state changes that might have occurred during the turn.
            await conversationState.SaveChangesAsync(turnContext, false, cancellationToken);
            await userState.SaveChangesAsync(turnContext, false, cancellationToken);
        }

        protected override async Task OnMessageActivityAsync(
            ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            // Run the Dialog with the new message Activity.
            await dialog.RunAsync(turnContext, conversationState.CreateProperty<DialogState>(nameof(DialogState)), cancellationToken);
        }

        protected override async Task OnMembersAddedAsync(
            IList<ChannelAccount> membersAdded, ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
        {
            foreach (var member in membersAdded)
            {
                if (member.Id != turnContext.Activity.Recipient.Id)
                {
                    var reply = MessageFactory.Text("Привет! Это бот учета рабочего времени GGroupp!");
                    await turnContext.SendActivityAsync(reply, cancellationToken);
                }
            }
        }
    }
}
