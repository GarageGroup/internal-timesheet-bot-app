using System;
using GarageGroup.Infra;
using GarageGroup.Infra.Bot.Builder;
using PrimeFuncPack;

namespace GarageGroup.Internal.Timesheet;

partial class Application
{
    internal static IBotBuilder UseAuthorizationFlow(this IBotBuilder botBuilder)
        =>
        Pipeline.Pipe(
            UseGraphHttpApi())
        .With(
            UseUserAuthorizationApi())
        .With(
            ResolveBotSignInOption)
        .MapSignInMiddleware(
            botBuilder);

    private static Dependency<IHttpApi> UseGraphHttpApi()
        =>
        PrimaryHandler.UseStandardSocketsHttpHandler()
        .UseLogging("GraphApi")
        .UsePollyStandard()
        .UseTokenCredentialStandard()
        .UseHttpApi();

    private static BotSignInOption ResolveBotSignInOption(IServiceProvider serviceProvider)
        =>
        new(
            oAuthConnectionName: serviceProvider.GetConfiguration()["OAuthConnectionName"].OrEmpty(),
            enterText: """
                Sign in to your Garage Group account:
                1. Follow the link
                2. Sign in with your Garage Group account
                3. Copy and send the received code to this chat
                """);
}