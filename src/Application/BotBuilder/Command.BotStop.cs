using GGroupp.Infra.Bot.Builder;

namespace GGroupp.Internal.Timesheet;

partial class GTimesheetBotBuilder
{
    internal static IBotBuilder UseGTimesheetBotStop(this IBotBuilder botBuilder)
        =>
        botBuilder.UseBotStop(StopCommand, static () => new(successText: "Операция остановлена"));
}