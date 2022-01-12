using System;
using GGroupp.Infra.Bot.Builder;

namespace GGroupp.Internal.Timesheet;

using ITimesheetCreateFunc = IAsyncValueFunc<TimesheetCreateIn, Result<TimesheetCreateOut, Failure<TimesheetCreateFailureCode>>>;

partial class BotDependency
{
    internal static ITimesheetCreateFunc GetTimesheetCreateApi(IBotContext botContext)
        =>
        CreateStandardHttpHandlerDependency("TimesheetCreateApi")
        .UseDataverseImpersonation(botContext)
        .CreateDataverseApiClient()
        .UseTimesheetCreateApi()
        .Resolve(botContext.ServiceProvider);
}