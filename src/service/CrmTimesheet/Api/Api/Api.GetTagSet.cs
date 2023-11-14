using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using GarageGroup.Infra;

namespace GarageGroup.Internal.Timesheet;

partial class CrmTimesheetApi<TDataverseApi>
{
    public ValueTask<Result<TimesheetTagSetGetOut, Failure<Unit>>> GetTagSetAsync(
        TimesheetTagSetGetIn input, CancellationToken cancellationToken)
        =>
        AsyncPipeline.Pipe(
            input ?? throw new ArgumentNullException(nameof(input)), cancellationToken)
        .Pipe(
            static @in => new DataverseEntitySetGetIn(
                entityPluralName: TimesheetItemJson.EntityPluralName,
                selectFields: TimesheetTagJson.SelectedFields,
                filter: TimesheetTagJson.BuildFilter(@in.UserId, @in.ProjectId, @in.MinDate),
                orderBy: TimesheetTagJson.OrderFields))
        .PipeValue(
            dataverseApi.Impersonate(input.UserId).GetEntitySetAsync<TimesheetTagJson>)
        .Map(
            static success => new TimesheetTagSetGetOut
            {
                Tags = success.Value.AsEnumerable().SelectMany(GetHashTags).Distinct().ToFlatArray()
            },
            static failure => failure.MapFailureCode(Unit.From));

    private static IEnumerable<string> GetHashTags(TimesheetTagJson timesheet)
    {
        if (string.IsNullOrWhiteSpace(timesheet.Description))
        {
            yield break;
        }

        foreach (var tagMatch in HashTagRegex.Matches(timesheet.Description).Cast<Match>())
        {
            yield return tagMatch.Value;
        }
    }
}