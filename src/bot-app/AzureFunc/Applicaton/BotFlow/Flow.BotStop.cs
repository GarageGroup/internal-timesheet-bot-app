using GGroupp.Infra.Bot.Builder;

namespace GGroupp.Internal.Timesheet;

partial class Application
{
    private static IBotBuilder UseBotStopFlow(this IBotBuilder botBuilder)
        =>
        botBuilder.UseBotStop(StopCommand, static () => new(successText: "Операция остановлена"));
}