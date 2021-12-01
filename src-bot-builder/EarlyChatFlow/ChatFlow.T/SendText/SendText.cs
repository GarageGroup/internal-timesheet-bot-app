using System;
using Microsoft.Bot.Builder;

namespace GGroupp.Infra.Bot.Builder;

partial class ChatFlow<T>
{
    public ChatFlow<T> SendText(Func<IChatFlowContext<T>, string> textFactory)
        =>
        InnerSendText(
            textFactory ?? throw new ArgumentNullException(nameof(textFactory)));

    private ChatFlow<T> InnerSendText(Func<IChatFlowContext<T>, string> textFactory)
        =>
        InnerSendActivity(
            context => MessageFactory.Text(text: textFactory.Invoke(context)));
}