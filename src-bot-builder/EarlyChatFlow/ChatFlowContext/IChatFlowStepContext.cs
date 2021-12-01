using Microsoft.Bot.Builder;

namespace GGroupp.Infra.Bot.Builder;

public interface IChatFlowStepContext : IStepStateSupplier, ITurnContext
{
}