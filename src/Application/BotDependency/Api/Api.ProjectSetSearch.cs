using System;
using GGroupp.Infra.Bot.Builder;

namespace GGroupp.Internal.Timesheet;

using IProjectSetSearchFunc = IAsyncValueFunc<ProjectSetSearchIn, Result<ProjectSetSearchOut, Failure<ProjectSetSearchFailureCode>>>;

partial class BotDependency
{
    internal static IProjectSetSearchFunc GetProjectSetSearchApi(IBotContext botContext)
        =>
        CreateStandardHttpHandlerDependency("ProjectSetSearchApi")
        .UseDataverseImpersonation(botContext)
        .CreateDataverseApiClient()
        .UseProjectSetSearchApi()
        .Resolve(botContext.ServiceProvider);
}