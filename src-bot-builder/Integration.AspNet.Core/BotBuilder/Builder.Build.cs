using Microsoft.Bot.Builder;
using Microsoft.Extensions.Logging;

namespace GGroupp.Infra.Bot.Builder;

partial class BotBuilder
{
    public IBot Build() => new BotImpl(conversationState, userState, loggerFactory.CreateLogger<BotImpl>(), middlewares);
}