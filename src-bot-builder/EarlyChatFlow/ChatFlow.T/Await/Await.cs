namespace GGroupp.Infra.Bot.Builder;

partial class ChatFlow<T>
{
    public ChatFlow<T> Await()
        =>
        InnerForward(
            context => context.StepState is null ? ChatFlowAction.AwaitAndRetry<T>(new()) : ChatFlowAction.Next(context.FlowState));
}