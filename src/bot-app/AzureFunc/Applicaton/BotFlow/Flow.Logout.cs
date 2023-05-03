using GGroupp.Infra.Bot.Builder;

namespace GGroupp.Internal.Timesheet;

partial class Application
{
    private static IBotBuilder UseLogoutFlow(this IBotBuilder botBuilder)
        =>
        botBuilder.UseLogout(LogoutCommand);
}