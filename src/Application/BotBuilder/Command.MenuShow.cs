using System;
using System.Threading;
using GGroupp.Infra.Bot.Builder;

namespace GGroupp.Internal.Timesheet;

partial class GTimesheetBotBuilder
{
    internal static IBotBuilder UseGTimesheetBotMenu(this IBotBuilder botBuilder)
        =>
        botBuilder.UseBotMenu(lazyMenuData.Value);

    private static readonly Lazy<BotMenuData> lazyMenuData = new(CreateMenuData, LazyThreadSafetyMode.ExecutionAndPublication);

    private static BotMenuData CreateMenuData()
        =>
        new(
            text: "Меню бота",
            commands: new BotMenuCommand[]
            {
                new(Guid.Parse("31f7730f-5d18-468c-b540-1cd03e27c268"), TimesheetCreateCommand, "Списать время"),
                new(Guid.Parse("49919c45-ef85-4fc9-a21d-8b5683303360"), BotInfoCommand, "О боте")
            });
}