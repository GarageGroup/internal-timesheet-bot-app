using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GGroupp.Infra.Bot.Builder;

namespace GGroupp.Internal.Timesheet;

using IProjectSetSearchFunc = IAsyncValueFunc<ProjectSetSearchIn, Result<ProjectSetSearchOut, Failure<ProjectSetSearchFailureCode>>>;

internal static class ProjectFindFlowStep
{
    private const string ProjectTypeFieldName = nameof(ProjectItemSearchOut.Type);

    private const int MaxProjectsCount = 5;

    internal static ChatFlow<TimesheetCreateFlowStateJson> FindProject(
        this ChatFlow<TimesheetCreateFlowStateJson> chatFlow,
        IProjectSetSearchFunc projectSetSearchFunc)
        =>
        chatFlow.SendText(
            _ => "Нужно выбрать проект. Введите часть названия для поиска")
        .AwaitLookupValue(
            (_, search, token) => projectSetSearchFunc.SearchProjectsAsync(search, token),
            (flowState, projectValue) => flowState with
            {
                ProjectType = projectValue.ParseProjectType(),
                ProjectId = projectValue.Id,
                ProjectName = projectValue.Name
            });

    private static ValueTask<Result<LookupValueSetSeachOut, ChatFlowStepFailure>> SearchProjectsAsync(
        this IProjectSetSearchFunc projectSetSearchFunc,
        LookupValueSetSeachIn seachInput,
        CancellationToken cancellationToken)
        =>
        AsyncPipeline.Pipe(
            seachInput, cancellationToken)
        .Pipe(
            @in => new ProjectSetSearchIn(
                searchText: @in.Text,
                top: MaxProjectsCount))
        .PipeValue(
            projectSetSearchFunc.InvokeAsync)
        .MapFailure(
            MapToStepFailure)
        .Filter(
            @out => @out.Projects.Any(),
            _ => ChatFlowStepFailure.FromUI("Ничего не найдено. Попробуйте уточнить запрос"))
        .MapSuccess(
            @out => new LookupValueSetSeachOut(
                items: @out.Projects.Select(MapProjectItem).ToArray(),
                choiceText: "Выберите проект"));

    private static LookupValue MapProjectItem(ProjectItemSearchOut item)
        =>
        new(
            item.Id,
            item.Name,
            new KeyValuePair<string, string>[]
            {
                new(ProjectTypeFieldName, item.Type.ToString("G"))
            });

    private static TimesheetProjectType ParseProjectType(this LookupValue lookupValue)
        =>
        lookupValue.Extensions.GetValueOrAbsent(ProjectTypeFieldName).Map(Enum.Parse<TimesheetProjectType>).OrDefault();

    private static ChatFlowStepFailure MapToStepFailure(Failure<ProjectSetSearchFailureCode> failure)
        =>
        new(
            uiMessage: "При поиске проектов произошла непредвиденная ошибка. Обратитесь к администратору или повторите попытку позднее",
            logMessage: failure.FailureMessage);
}