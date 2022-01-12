using System;
using GGroupp.Infra.Bot.Builder;
using GGroupp.Platform;

namespace GGroupp.Internal.Timesheet;

using IDataverseUserGetFunc = IAsyncValueFunc<DataverseUserGetIn, Result<DataverseUserGetOut, Failure<DataverseUserGetFailureCode>>>;

partial class BotDependency
{
    internal static IDataverseUserGetFunc GetDataverseUserGetApi(IBotContext botContext)
        =>
        CreateStandardHttpHandlerDependency("DataverseUserGetApi")
        .CreateDataverseApiClient()
        .UseUserGetApi()
        .Resolve(botContext.ServiceProvider);
}