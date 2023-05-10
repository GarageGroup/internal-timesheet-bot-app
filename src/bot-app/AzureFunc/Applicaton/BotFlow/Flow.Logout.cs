using GarageGroup.Infra.Bot.Builder;

namespace GarageGroup.Internal.Timesheet;

partial class Application
{
    private static IBotBuilder UseLogoutFlow(this IBotBuilder botBuilder)
        =>
        botBuilder.UseLogout(LogoutCommand);
}