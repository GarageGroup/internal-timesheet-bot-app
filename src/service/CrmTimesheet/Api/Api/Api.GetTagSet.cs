using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using GarageGroup.Infra;

namespace GarageGroup.Internal.Timesheet;

partial class CrmTimesheetApi
{
    public ValueTask<Result<TimesheetTagSetGetOut, Failure<Unit>>> GetTagSetAsync(
        TimesheetTagSetGetIn input, CancellationToken cancellationToken)
        =>
        AsyncPipeline.Pipe(
            input, cancellationToken)
        .Pipe(
            static @in => DbTimesheetTag.QueryAll with
            {
                Filter = new DbCombinedFilter(DbLogicalOperator.And)
                {
                    Filters =
                    [
                        DbTimesheetTag.BuildOwnerFilter(@in.UserId),
                        DbTimesheetTag.BuildProjectFilter(@in.ProjectId),
                        DescriptionTagFilter,
                        DbTimesheetTag.BuildMinDateFilter(@in.MinDate),
                        DbTimesheetTag.BuildMaxDateFilter(@in.MaxDate)
                    ]
                },
                Orders = DbTimesheetTag.DefaultOrders
            })
        .PipeValue(
            sqlApi.QueryEntitySetOrFailureAsync<DbTimesheetTag>)
        .MapSuccess(
            static success => new TimesheetTagSetGetOut
            {
                Tags = success.AsEnumerable().SelectMany(GetHashTags).Distinct().ToFlatArray()
            });

    private static IEnumerable<string> GetHashTags(DbTimesheetTag timesheet)
    {
        if (string.IsNullOrWhiteSpace(timesheet.Description))
        {
            yield break;
        }

        foreach (var tagMatch in TagRegex.Matches(timesheet.Description).Cast<Match>())
        {
            yield return tagMatch.Value;
        }
    }
}