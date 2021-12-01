using System;
using System.Threading;
using PrimeFuncPack;

namespace GGroupp.Internal.Timesheet;

using IProjectSetSearchFunc = IAsyncValueFunc<ProjectSetSearchIn, Result<ProjectSetSearchOut, Failure<ProjectSetSearchFailureCode>>>;

partial class BotDependency
{
    private static readonly Lazy<Dependency<IProjectSetSearchFunc>> projectSetSearchApiDependency
        =
        new(CreateProjectSetSearchApiDependency, LazyThreadSafetyMode.ExecutionAndPublication);

    internal static IProjectSetSearchFunc ResolveProjectSetSearchApi(IServiceProvider serviceProvider)
        =>
        projectSetSearchApiDependency.Value.Resolve(serviceProvider);

    private static Dependency<IProjectSetSearchFunc> CreateProjectSetSearchApiDependency()
        =>
        CreateDataverseApiDependency("ProjectSetSearchApi").UseProjectSetSearchApi();
}