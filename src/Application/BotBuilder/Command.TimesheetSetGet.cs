using System;
using GGroupp.Infra.Bot.Builder;

namespace GGroupp.Internal.Timesheet;

using ITimesheetSetGetFunc = IAsyncValueFunc<TimesheetSetGetIn, Result<TimesheetSetGetOut, Failure<TimesheetSetGetFailureCode>>>;

partial class GTimesheetBotBuilder
{
    internal static IBotBuilder UseGTimesheetSetGet(this IBotBuilder botBuilder, string commandName)
        =>
        botBuilder.UseTimesheetSetGet(commandName, GetTimesheetSetApi);

    private static ITimesheetSetGetFunc GetTimesheetSetApi(IBotContext botContext)
        =>
        CreateStandardHttpHandlerDependency("TimesheetSetGetApi")
        .UseDataverseImpersonation(botContext)
        .CreateDataverseApiClient()
        .UseTimesheetSetGetApi()
        .Resolve(botContext.ServiceProvider);
}