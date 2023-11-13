using System;
using System.Threading;
using System.Threading.Tasks;

namespace GarageGroup.Internal.Timesheet;

public interface ICrmTimesheetApi
{
    ValueTask<Result<TimesheetTagSetGetOut, Failure<Unit>>> GetTagSetAsync(
        TimesheetTagSetGetIn input, CancellationToken cancellationToken);

    ValueTask<Result<TimesheetSetGetOut, Failure<TimesheetSetGetFailureCode>>> GetAsync(
        TimesheetSetGetIn input, CancellationToken cancellationToken);

    ValueTask<Result<Unit, Failure<TimesheetCreateFailureCode>>> CreateAsync(
        TimesheetCreateIn input, CancellationToken cancellationToken);
}