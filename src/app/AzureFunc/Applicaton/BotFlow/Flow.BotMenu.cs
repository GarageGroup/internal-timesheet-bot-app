using System;
using GarageGroup.Infra.Bot.Builder;

namespace GarageGroup.Internal.Timesheet;

partial class Application
{
    private static IBotBuilder UseBotMenuFlow(this IBotBuilder botBuilder)
        =>
        botBuilder.UseBotMenu(CreateMenuData());

    private static BotMenuData CreateMenuData()
        =>
        new(
            text: "Bot commands",
            commands:
            [
                new(Guid.Parse("31f7730f-5d18-468c-b540-1cd03e27c268"), TimesheetCreateCommand, "Create timesheet"),
                new(Guid.Parse("a5622d66-5b63-4d3b-a0c6-c5123ac8e538"), DateTimesheetGetCommand, "Show timesheets"),
                new(Guid.Parse("49919c45-ef85-4fc9-a21d-8b5683303360"), BotInfoCommand, "Information about the bot")
            ]);
}
