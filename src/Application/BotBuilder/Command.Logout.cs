using GGroupp.Infra.Bot.Builder;

namespace GGroupp.Internal.Timesheet;

partial class GTimesheetBotBuilder
{
    internal static IBotBuilder UseGTimesheetLogout(this IBotBuilder botBuilder)
        =>
        botBuilder.UseLogout(LogoutCommand);
}