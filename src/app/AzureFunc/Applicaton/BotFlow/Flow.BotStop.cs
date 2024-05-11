using GarageGroup.Infra.Bot.Builder;

namespace GarageGroup.Internal.Timesheet;

partial class Application
{
    private static IBotBuilder UseBotStopFlow(this IBotBuilder botBuilder)
        =>
        botBuilder.UseBotStop(StopCommand, static () => new(successText: "Operation has been stopped"));
}
