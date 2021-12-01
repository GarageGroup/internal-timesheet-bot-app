using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;

namespace GGroupp.Infra.Bot.Builder;

partial class ChatFlow<T>
{
    public ChatFlow<T> SendTextValue(Func<IChatFlowContext<T>, CancellationToken, ValueTask<string>> textFactoryAsync)
        =>
        InnerSendTextValue(
            textFactoryAsync ?? throw new ArgumentNullException(nameof(textFactoryAsync)));

    private ChatFlow<T> InnerSendTextValue(Func<IChatFlowContext<T>, CancellationToken, ValueTask<string>> textFactoryAsync)
        =>
        InnerSendActivityValue(
            async (context, token) => MessageFactory.Text(
                text: await textFactoryAsync.Invoke(context, token).ConfigureAwait(false)));
}