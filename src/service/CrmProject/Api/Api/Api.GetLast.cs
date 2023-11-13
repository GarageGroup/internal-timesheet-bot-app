using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GarageGroup.Infra;

namespace GarageGroup.Internal.Timesheet;

partial class CrmProjectApi<TDataverseApi>
{
    public ValueTask<Result<LastProjectSetGetOut, Failure<ProjectSetGetFailureCode>>> GetLastAsync(
        LastProjectSetGetIn input, CancellationToken cancellationToken)
        =>
        AsyncPipeline.Pipe(
            input, cancellationToken)
        .Pipe(
            @in => new DataverseEntitySetGetIn(
                entityPluralName: BaseTimesheetItemJson.EntityPluralName,
                selectFields: LastTimesheetItemJson.SelectedFields,
                expandFields: BaseTimesheetItemJson.ExpandedFields,
                orderBy: LastTimesheetItemJson.OrderFiels,
                filter: BuildFilter(@in),
                top: option.LastProjectItemsCount))
        .PipeValue(
            dataverseApi.Impersonate(input.UserId).GetEntitySetAsync<LastTimesheetItemJson>)
        .Map(
            success => new LastProjectSetGetOut
            {
                Projects = GetProjects(success.Value, input.Top)
            },
            static failure => failure.MapFailureCode(MapFailureCode));

    private string BuildFilter(LastProjectSetGetIn input)
    {
        var today = todayProvider.GetToday();
        var maxDate = today.AddDays(1);

        DateOnly? minDate = option.LastProjectDaysCount switch
        {
            not null => today.AddDays(option.LastProjectDaysCount.Value * -1),
            _ => null
        };

        return LastTimesheetItemJson.BuildFilter(input.UserId, maxDate, minDate);
    }

    private static FlatArray<ProjectSetGetItem> GetProjects(
        FlatArray<LastTimesheetItemJson> itemsJson, int? top)
        =>
        itemsJson.AsEnumerable()
        .Select(
            static item => item.GetProjectType())
        .NotNull()
        .GroupBy(
            static type => type.Id)
        .Select(
            static item => item.First())
        .Select(
            MapProjectItem)
        .TakeTop(
            top)
        .ToFlatArray();
}