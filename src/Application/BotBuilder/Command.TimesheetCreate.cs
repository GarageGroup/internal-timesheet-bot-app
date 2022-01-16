using System;
using GGroupp.Infra.Bot.Builder;

namespace GGroupp.Internal.Timesheet;

using IProjectSetSearchFunc = IAsyncValueFunc<ProjectSetSearchIn, Result<ProjectSetSearchOut, Failure<ProjectSetSearchFailureCode>>>;
using ITimesheetCreateFunc = IAsyncValueFunc<TimesheetCreateIn, Result<TimesheetCreateOut, Failure<TimesheetCreateFailureCode>>>;

partial class GTimesheetBotBuilder
{
    internal static IBotBuilder UseGTimesheetCreate(this IBotBuilder botBuilder, string commandName)
        =>
        botBuilder.UseTimesheetCreate(commandName, GetProjectSetSearchApi, GetTimesheetCreateApi);

    private static IProjectSetSearchFunc GetProjectSetSearchApi(IBotContext botContext)
        =>
        CreateStandardHttpHandlerDependency("ProjectSetSearchApi")
        .UseDataverseImpersonation(botContext)
        .CreateDataverseApiClient()
        .UseProjectSetSearchApi()
        .Resolve(botContext.ServiceProvider);

    private static ITimesheetCreateFunc GetTimesheetCreateApi(IBotContext botContext)
        =>
        CreateStandardHttpHandlerDependency("TimesheetCreateApi")
        .UseDataverseImpersonation(botContext)
        .CreateDataverseApiClient()
        .UseTimesheetCreateApi()
        .Resolve(botContext.ServiceProvider);
}