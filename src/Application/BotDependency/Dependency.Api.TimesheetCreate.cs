using System;
using System.Threading;
using System.Threading.Tasks;
using PrimeFuncPack;

namespace GGroupp.Internal.Timesheet;

using ITimesheetCreateFunc = IAsyncValueFunc<TimesheetCreateIn, Result<TimesheetCreateOut, Failure<TimesheetCreateFailureCode>>>;

partial class BotDependency
{
    /*private static readonly Lazy<Dependency<ITimesheetCreateFunc>> timesheetCreateApiDependency
        =
        new(CreateTimesheetCreateApiDependency, LazyThreadSafetyMode.ExecutionAndPublication);*/

    internal static ITimesheetCreateFunc ResolveTimesheetCreateApi(IServiceProvider _)
        =>
        AsyncValueFunc.From<TimesheetCreateIn, Result<TimesheetCreateOut, Failure<TimesheetCreateFailureCode>>>(
            CreateTimesheetAsync);
        //timesheetCreateApiDependency.Value.Resolve(serviceProvider);

    /*private static Dependency<ITimesheetCreateFunc> CreateTimesheetCreateApiDependency()
        =>
        AsyncValueFunc.From<TimesheetCreateIn, Result<TimesheetCreateOut, Failure<TimesheetCreateFailureCode>>>(
            CreateTimesheetAsync)
        .Pipe(
            Dependency.Of);
        CreateDataverseApiDependency("TimesheetCreateApi").UseTimesheetCreateApi();*/

    private static ValueTask<Result<TimesheetCreateOut, Failure<TimesheetCreateFailureCode>>> CreateTimesheetAsync(
        TimesheetCreateIn input, CancellationToken cancellationToken)
        =>
        AsyncPipeline.Pipe(
            input, cancellationToken)
        .Pipe<Result<TimesheetCreateOut, Failure<TimesheetCreateFailureCode>>>(
            @in => @in.Description switch
            {
                "fail" => Failure.Create(TimesheetCreateFailureCode.Unknown, "Test failure"),
                _ => new TimesheetCreateOut(Guid.NewGuid())
            });
}