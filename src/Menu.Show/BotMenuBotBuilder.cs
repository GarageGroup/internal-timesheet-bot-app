using System;
using System.Threading;
using System.Threading.Tasks;
using GGroupp.Infra.Bot.Builder;

namespace GGroupp.Internal.Timesheet;

public static class BotMenuBotBuilder
{
    public static IBotBuilder UseBotMenu(this IBotBuilder botBuilder, BotMenuData menuData)
    {
        _ = botBuilder ?? throw new ArgumentNullException(nameof(botBuilder));
        _ = menuData ?? throw new ArgumentNullException(nameof(menuData));

        return botBuilder.Use(InnerInvokeAsync);

        ValueTask<Unit> InnerInvokeAsync(IBotContext botContext, CancellationToken cancellationToken)
            =>
            botContext.InvokeCommandAsync(menuData, cancellationToken);
    }
}