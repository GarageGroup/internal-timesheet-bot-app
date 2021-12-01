using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;

namespace GGroupp.Infra.Bot.Builder;

partial class ChatFlow<T>
{
    public ChatFlow<T> SendText(Func<IChatFlowContext<T>, CancellationToken, Task<string>> textFactoryAsync)
        =>
        InnerSendText(
            textFactoryAsync ?? throw new ArgumentNullException(nameof(textFactoryAsync)));

    private ChatFlow<T> InnerSendText(Func<IChatFlowContext<T>, CancellationToken, Task<string>> textFactoryAsync)
        =>
        InnerSendActivityValue(
            async (context, token) => MessageFactory.Text(
                text: await textFactoryAsync.Invoke(context, token).ConfigureAwait(false)));
}