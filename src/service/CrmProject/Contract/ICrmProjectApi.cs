using System;
using System.Threading;
using System.Threading.Tasks;

namespace GarageGroup.Internal.Timesheet;

public interface ICrmProjectApi
{
    ValueTask<Result<LastProjectSetGetOut, Failure<ProjectSetGetFailureCode>>> GetLastAsync(
        LastProjectSetGetIn input, CancellationToken cancellationToken);

    ValueTask<Result<ProjectSetSearchOut, Failure<ProjectSetGetFailureCode>>> SearchAsync(
        ProjectSetSearchIn input, CancellationToken cancellationToken);
}