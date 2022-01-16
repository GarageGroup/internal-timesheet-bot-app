using GGroupp.Infra.Bot.Builder;

namespace GGroupp.Internal.Timesheet;

partial class GTimesheetBotBuilder
{
    internal static IBotBuilder UseGTimesheetBotStop(this IBotBuilder botBuilder, string commandName)
        =>
        botBuilder.UseBotStop(commandName, () => new(successText: "Операция остановлена"));
}