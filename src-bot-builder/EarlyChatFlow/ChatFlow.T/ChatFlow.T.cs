namespace GGroupp.Infra.Bot.Builder;

public sealed partial class ChatFlow<T>
{
    private readonly ChatFlowEngine<T> chatFlowEngine;

    internal ChatFlow(ChatFlowEngine<T> chatFlowEngine)
        =>
        this.chatFlowEngine = chatFlowEngine;
}