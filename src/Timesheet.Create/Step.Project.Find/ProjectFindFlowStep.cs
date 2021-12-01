using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GGroupp.Infra.Bot.Builder;
using Microsoft.Extensions.Logging;

namespace GGroupp.Internal.Timesheet;

using IProjectSetSearchFunc = IAsyncValueFunc<ProjectSetSearchIn, Result<ProjectSetSearchOut, Failure<ProjectSetSearchFailureCode>>>;

internal static class ProjectFindFlowStep
{
    private const string ProjectTypeFieldName = nameof(ProjectItemSearchOut.Type);

    private const int MaxProjectsCount = 5;

    internal static ChatFlow<TimesheetCreateFlowStateJson> FindProject(
        this ChatFlow<TimesheetCreateFlowStateJson> chatFlow,
        IProjectSetSearchFunc projectSetSearchFunc,
        ILogger logger)
        =>
        chatFlow.SendText(
            _ => "Нужно выбрать проект. Введите часть названия для поиска")
        .AwaitLookupValue(
            (_, search, token) => projectSetSearchFunc.SearchProjectsAsync(logger, search, token),
            (flowState, projectValue) => flowState with
            {
                ProjectType = ParseProjectType(projectValue),
                ProjectId = projectValue.Id,
                ProjectName = projectValue.Name
            });

    private static ValueTask<Result<LookupValueSetSeachOut, Failure<Unit>>> SearchProjectsAsync(
        this IProjectSetSearchFunc projectSetSearchFunc,
        ILogger logger,
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
            logger.LogFailure)
        .MapFailure(
            CreateProjectSetSearchUIFailure)
        .Filter(
            @out => @out.Projects.Any(),
            _ => Failure.Create("Ничего не найдено. Попробуйте уточнить запрос"))
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

    private static TimesheetCreateFlowProjectType ParseProjectType(this LookupValue lookupValue)
        =>
        Pipeline.Pipe(
            lookupValue.Extensions)
        .GetValueOrAbsent(
            ProjectTypeFieldName)
        .Map(
            Enum.Parse<ProjectTypeSearchOut>)
        .Map(
            MapProjectType)
        .OrDefault();

    private static TimesheetCreateFlowProjectType MapProjectType(ProjectTypeSearchOut projectType)
        =>
        projectType switch
        {
            ProjectTypeSearchOut.Lead => TimesheetCreateFlowProjectType.Lead,
            ProjectTypeSearchOut.Opportunity => TimesheetCreateFlowProjectType.Opportunity,
            _ => TimesheetCreateFlowProjectType.Project
        };

    private static Failure<Unit> CreateProjectSetSearchUIFailure(Unit _)
        =>
        Failure.Create("При поиске проектов произошла непредвиденная ошибка. Обратитесь к администратору или повторите попытку позднее");
}