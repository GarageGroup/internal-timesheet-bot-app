namespace GGroupp.Infra.Bot.Builder;

public enum ChatFlowActionCode
{
    Interruption,

    Canceling,

    AwaitingAndRetry,

    Next
}