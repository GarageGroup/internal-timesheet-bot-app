using System;
using GarageGroup.Infra.Bot.Builder;

namespace GarageGroup.Internal.Timesheet;

partial class Application
{
    private static IBotBuilder UseLogoutFlow(this IBotBuilder botBuilder)
        =>
        Pipeline.Pipe(
            UseUserAuthorizationApi())
        .With(
            CreateBotSignOutOption)
        .MapSignOutCommand(
            botBuilder, LogoutCommand);

    private static BotSignOutOption CreateBotSignOutOption()
        =>
        new();
}