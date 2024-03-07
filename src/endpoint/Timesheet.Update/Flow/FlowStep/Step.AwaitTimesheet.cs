using GarageGroup.Infra.Bot.Builder;
using Microsoft.Bot.Builder;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace GarageGroup.Internal.Timesheet;
partial class TimesheetUpdateFlowStep
{
    internal static ChatFlow<UpdateTimesheetFlowState> AwaitTimesheetWebApp(this ChatFlow<UpdateTimesheetFlowState> chatFlow, ICrmProjectApi crmProjectApi)
        =>
        chatFlow.ForwardValue(crmProjectApi.SelectTimesheet);

    private static async ValueTask<ChatFlowJump<UpdateTimesheetFlowState>> SelectTimesheet(
        this ICrmProjectApi crmProjectApi,
        IChatFlowContext<UpdateTimesheetFlowState> context, 
        CancellationToken token)
    {
        var turnContext = (ITurnContext)context;
        var cache = context.StepState as DateWebAppCacheJson;

        if (cache != null)
        {
            var json = JsonConvert.SerializeObject(context.Activity.ChannelData);
            var channelDataResponse = JsonConvert.DeserializeObject<ChannelDataResposeJson>(json);

            return (cache, channelDataResponse) switch
            {
                ({ Status: UpdateStatus.EditTimesheet }, { Message: { Text: null } }) => await ProcessingMessagesWebApp(crmProjectApi, context, cache, token).ConfigureAwait(false),
                ({ Status: UpdateStatus.EditingProject }, { CallbackQuery: { } }) => await EditProjectForLasted(context, cache, channelDataResponse, token).ConfigureAwait(false),
                ({ Status: UpdateStatus.EditingProject }, { Message: { } }) => await SelectProjectForSearch(context, crmProjectApi, cache, channelDataResponse.Message, token).ConfigureAwait(false),
                ({ Status: UpdateStatus.ProjectIsEdited, EditedProject: { } }, { Message: { } }) => context.FlowState with
                {
                    TimesheetUpdate = cache.EditTimesheetState,
                    ProjectId = cache.EditedProject.Id,
                    ProjectName = cache.EditedProject.Name,
                    ProjectType = GetProjectType(cache.EditedProject),
                    UpdateProject = true
                },
                _ => ChatFlowJump.Break<UpdateTimesheetFlowState>(ChatFlowBreakState.From(UnknownErrorText))
            };
        }

        return await SelectDate(context, turnContext, token).ConfigureAwait(false);
    }

    private static async Task<ChatFlowJump<UpdateTimesheetFlowState>> ProcessingMessagesWebApp(
        ICrmProjectApi crmProjectApi, 
        IChatFlowContext<UpdateTimesheetFlowState> context, 
        DateWebAppCacheJson cache, 
        CancellationToken token)
        =>
        await AsyncPipeline
            .Pipe(
                context, token)
            .Pipe(
                _ => ParseTimesheet(context))
            .MapSuccess(
                data => (context, data, crmProjectApi, cache))
            .FoldValue(
                SuccessSelectTimesheetAsync,
                async (botFailure, cancellationToken) =>
                {
                    var activity = MessageFactory.Text(botFailure.UserMessage);
                    await SendInsteadActivityAsync(context, cache.ActivityId, activity, cancellationToken).ConfigureAwait(false);
                    return context.RepeatSameStateJump<UpdateTimesheetFlowState>();
                })
            .ToValueTask();


    private static async ValueTask<ChatFlowJump<UpdateTimesheetFlowState>> SelectDate(
        IChatFlowContext<UpdateTimesheetFlowState> context, ITurnContext turnContext, CancellationToken token)
    {
        var activity = MessageFactory.Text("Выбери списание времени, которое нужно изменить");

        var webAppData = new WebAppTimesheetsDataJson
        {
            Date = context.FlowState.Date.ToString(),
            Timesheets = context.FlowState.Timesheets,
        };

        var webAppDataJson = JsonConvert.SerializeObject(webAppData);
        var base64Timesheets = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(webAppDataJson));

        activity.ChannelData = CreateChannelDataSelectTimesheet(context.FlowState.Options.UrlWebApp, base64Timesheets);

        var resource = await turnContext.SendActivityAsync(activity, token).ConfigureAwait(false);

        return ChatFlowJump.Repeat<UpdateTimesheetFlowState>(new DateWebAppCacheJson(resource?.Id));
    }

    private static async ValueTask<ChatFlowJump<UpdateTimesheetFlowState>> EditProjectForLasted(
        IChatFlowContext<UpdateTimesheetFlowState> context,
        DateWebAppCacheJson cache,
        ChannelDataResposeJson channelDataResponse,
        CancellationToken token)
    {
        var project = cache.Projects?.ToList().FirstOrDefault(x => x.Id == channelDataResponse?.CallbackQuery?.ProjectId);

        if (project is null)
        {
            return ChatFlowJump.Break<UpdateTimesheetFlowState>(ChatFlowBreakState.From(UnknownErrorText));
        }
        if (cache.EditTimesheetState is null)
        {
            return ChatFlowJump.Break<UpdateTimesheetFlowState>(ChatFlowBreakState.From(UnknownErrorText));
        }

        var webAppData = cache.EditTimesheetState with
        {
            ProjectName = project.Name
        };

        var webAppDataJson = JsonConvert.SerializeObject(webAppData);
        var base64Timesheet = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(webAppDataJson));

        return await SendActivityProgectChange(context, cache, project, base64Timesheet, token).ConfigureAwait(false);
    }

    private static async ValueTask<ChatFlowJump<UpdateTimesheetFlowState>> SendActivityProgectChange(
        IChatFlowContext<UpdateTimesheetFlowState> context,
        DateWebAppCacheJson cache, 
        ProjectCacheJson project,
        string base64Timesheet,
        CancellationToken token)
    {
        var turnContext = (ITurnContext)context;
        await turnContext.DeleteActivityAsync(cache.ActivityId, token).ConfigureAwait(false);

        var activityEditProject = MessageFactory.Text($"Проект успешно сменен на {project.Name}");
        activityEditProject.ChannelData = CreateChannelDataEditTimesheetForm(context.FlowState.Options.UrlWebApp, base64Timesheet);

        var resource = await turnContext.SendActivityAsync(activityEditProject, token).ConfigureAwait(false);

        return ChatFlowJump.Repeat<UpdateTimesheetFlowState>(
            new DateWebAppCacheJson(resource.Id, UpdateStatus.ProjectIsEdited, default, project, cache.EditTimesheetState));
    }

    private static async ValueTask<ChatFlowJump<UpdateTimesheetFlowState>> SuccessSelectTimesheetAsync(
        (IChatFlowContext<UpdateTimesheetFlowState> Context, UpdateTimesheetJson Timesheet, ICrmProjectApi CrmProjectApi, DateWebAppCacheJson Cache) input,
        CancellationToken cancellationToken = default)
    {
        return (input.Timesheet, input.Cache) switch
        {
            { Timesheet: { IsEditProject: true } } => await SelectProjectForLasted(input, cancellationToken).ConfigureAwait(false),
            { Cache: { Status: UpdateStatus.ProjectIsEdited, EditedProject: { } } } => input.Context.FlowState with
                {
                    TimesheetUpdate = input.Timesheet,
                    ProjectId = input.Cache.EditedProject.Id,
                    ProjectName = input.Cache.EditedProject.Name,
                    ProjectType = GetProjectType(input.Cache.EditedProject),
                    UpdateProject = true
                },
            _ => input.Context.FlowState with
                {
                    TimesheetUpdate = input.Timesheet
                }
        };
    }

    private static ValueTask<ChatFlowJump<UpdateTimesheetFlowState>> SelectProjectForLasted(
        (IChatFlowContext<UpdateTimesheetFlowState> Context, UpdateTimesheetJson Timesheet, ICrmProjectApi CrmProjectApi, DateWebAppCacheJson Cache) input, 
        CancellationToken cancellationToken)
        =>
        AsyncPipeline.Pipe(
            input.Context, cancellationToken)
        .PipeValue(
            input.CrmProjectApi.GetLastProjectsAsync)
        .PipeValue(
            (lastProjects, token) => SendActivitySelectProject(input.Context, input.Timesheet, lastProjects.Items, input.Cache.ActivityId, token));
    

    private static async ValueTask<ChatFlowJump<UpdateTimesheetFlowState>> SelectProjectForSearch(
        IChatFlowContext<UpdateTimesheetFlowState> context, 
        ICrmProjectApi crmProjectApi, 
        DateWebAppCacheJson cache,
        MessageResponseJson inputText,
        CancellationToken cancellationToken)
    {
        if (inputText.Text is null || cache.EditTimesheetState is null)
        {
            return ChatFlowJump.Break<UpdateTimesheetFlowState>(ChatFlowBreakState.From(UnknownErrorText));
        }
        var projects = await crmProjectApi.SearchProjectsAsync(context, inputText.Text, cancellationToken);

        if (projects.IsFailure)
        {
            var activity = MessageFactory.Text(projects.FailureOrThrow().UserMessage);
            await SendInsteadActivityAsync(context, cache.ActivityId, activity, cancellationToken);
            return context.RepeatSameStateJump<UpdateTimesheetFlowState>();
        }

        var searchProjects = projects.SuccessOrThrow();

        return await SendActivitySelectProject(context, cache.EditTimesheetState, searchProjects.Items, cache.ActivityId, cancellationToken).ConfigureAwait(false);
    }

    private static async ValueTask<ChatFlowJump<UpdateTimesheetFlowState>> SendActivitySelectProject(
        IChatFlowContext<UpdateTimesheetFlowState> context, 
        UpdateTimesheetJson timesheet,
        FlatArray<LookupValue> projects,
        string? activityId,
        CancellationToken cancellationToken)
    {
        var turnContext = (ITurnContext)context;
        var projectsList = new List<ProjectCacheJson>();

        foreach (var project in projects)
        {
            projectsList.Add(new ProjectCacheJson
            {
                Id = project.Id,
                Name = project.Name,
                Data = project.Data,
            });
        }

        await turnContext.DeleteActivityAsync(activityId, cancellationToken);
        var activity = MessageFactory.Text(ChooseProjectMessage);
        activity.ChannelData = CreateChannelDataEditProject(projects);
        var resource = await turnContext.SendActivityAsync(activity, cancellationToken).ConfigureAwait(false);

        var editableTimesheet = context.FlowState.Timesheets?.First(t => t.Id == timesheet.Id);
        if (editableTimesheet is null)
        {
            return ChatFlowJump.Break<UpdateTimesheetFlowState>(ChatFlowBreakState.From(UnknownErrorText));
        }

        var editTimesheet = new UpdateTimesheetJson
        {
            Id = timesheet.Id,
            Description = timesheet.Description ?? editableTimesheet.Description,
            Duration = timesheet?.Duration ?? editableTimesheet.Duration,
            IsEditProject = true
        };

        return ChatFlowJump.Repeat<UpdateTimesheetFlowState>(
            new DateWebAppCacheJson(resource.Id, UpdateStatus.EditingProject, projectsList, null, editTimesheet));
    }

    private static ChannelDataJson CreateChannelDataEditProject(FlatArray<LookupValue> projects)
    {
        var buttons = new List<List<InlineKeyboardButtonJson>>();
        foreach (var project in projects)
        {
            buttons.Add(new List<InlineKeyboardButtonJson>
            {
                new InlineKeyboardButtonJson
                {
                    Text = project.Name,
                    CallbackData = project.Id.ToString()
                }
            });
        }

        return new ChannelDataJson
        {
            Method = "sendMessage",
            Parameters = new ParameterJson
            {
                ReplyMarkup = new InlineKeyboardMarkupJson
                {
                    InlineKeyboard = buttons.Select(b => b.ToArray()).ToArray()
                },
            }
        };
    }

    private static Result<UpdateTimesheetJson, BotFlowFailure> ParseTimesheet(IChatFlowContext<UpdateTimesheetFlowState> context)
    {
        var json = JsonConvert.SerializeObject(context.Activity.ChannelData);
        var dataWebApp = JsonConvert.DeserializeObject<WebAppResponseJson>(json);

        if (string.IsNullOrEmpty(dataWebApp?.Message?.WebAppData?.Data))
        {
            return BotFlowFailure.From("Выберете списание времени через сайт (кнопка ниже)");
        }

        var timesheet = JsonConvert.DeserializeObject<UpdateTimesheetJson>(dataWebApp.Message.WebAppData.Data);

        if (timesheet != null)
        {
            return timesheet;
        }
        else
        {
            return BotFlowFailure.From("Не удалось получить списание времени!");
        }
    }

    private static object CreateChannelDataEditTimesheetForm(string url, string data)
        =>
        new WebAppChannelDataJson
        {
            Method = "sendMessage",
            Parameters = new ParametersJson
            {
                ReplyMarkup = new ReplyMarkupJson
                {
                    KeyboardButtons =
                    [
                        [
                            new KeyboardButtonJson
                            {
                                Text = "Продолжить редактирование",
                                WebApp = new WebAppJson
                                {
                                    Url = $"{url}/updateTimesheetForm?data={data}"
                                }
                            },
                            new KeyboardButtonJson
                            {
                                Text = "Закончить редактирование"
                            }
                        ]
                    ],
                    ResizeKeyboard = true,
                    InputFieldPlaceholder = "Выберете следующий шаг"
                }
            }
        };

    private static object CreateChannelDataSelectTimesheet(string url, string data)
        =>
        new WebAppChannelDataJson
        {
            Method = "sendMessage",
            Parameters = new ParametersJson
            {
                ReplyMarkup = new ReplyMarkupJson
                {
                    KeyboardButtons =
                    [
                        [
                            new KeyboardButtonJson
                            {
                                Text = "Выбрать списание",
                                WebApp = new WebAppJson
                                {
                                    Url = $"{url}/selectUpdateTimesheet?data={data}"
                                }
                            }
                        ]
                    ],
                    ResizeKeyboard = true,
                    InputFieldPlaceholder = "Чтобы выбрать списание, нажмите кнопку"
                }
            }
        };

    private static ValueTask<LookupValueSetOption> GetLastProjectsAsync(
        this ICrmProjectApi crmProjectApi, IChatFlowContext<UpdateTimesheetFlowState> context, CancellationToken cancellationToken)
        =>
        AsyncPipeline.Pipe(
            context.FlowState, cancellationToken)
        .HandleCancellation()
        .Pipe(
            static flowState => new LastProjectSetGetIn(
                userId: flowState.UserId,
                top: MaxProjectsCount,
                minDate: GetDateUtc(-ProjectDays)))
        .PipeValue(
            crmProjectApi.GetLastAsync)
        .Fold(
            static @out => new(
                items: @out.Projects.Map(MapProjectItem),
                choiceText: @out.Projects.IsNotEmpty ? ChooseProjectMessage : DefaultProjectMessage),
            context.LogFailure);

    private static ValueTask<Result<LookupValueSetOption, BotFlowFailure>> SearchProjectsAsync(
        this ICrmProjectApi crmProjectApi, IChatFlowContext<UpdateTimesheetFlowState> context, string searchText, CancellationToken token)
        =>
        AsyncPipeline.Pipe(
            context.FlowState, token)
        .HandleCancellation()
        .Pipe(
            flowState => new ProjectSetSearchIn(
                searchText: searchText,
                userId: flowState.UserId,
                top: MaxProjectsCount))
        .PipeValue(
            crmProjectApi.SearchAsync)
        .MapFailure(
            MapToFlowFailure)
        .Filter(
            static @out => @out.Projects.IsNotEmpty,
            static _ => BotFlowFailure.From("Ничего не найдено. Попробуйте уточнить запрос"))
        .MapSuccess(
            static @out => new LookupValueSetOption(
                items: @out.Projects.Map(MapProjectItem),
                choiceText: "Выберите проект"));

    private static LookupValue MapProjectItem(ProjectSetGetItem item)
        =>
        new(item.Id, item.Name, item.Type.ToString("G"));

    private static LookupValueSetOption LogFailure(this ILoggerSupplier context, Failure<ProjectSetGetFailureCode> failure)
    {
        context.Logger.LogError(
            failure.SourceException,
            "GetLastProjects failure: {failureCode} {failureMessage}",
            failure.FailureCode,
            failure.FailureMessage);

        return new(default, DefaultProjectMessage, default);
    }

    private static TimesheetProjectType GetProjectType(this ProjectCacheJson projectValue)
        =>
        Enum.Parse<TimesheetProjectType>(projectValue.Data.OrEmpty());

    private static BotFlowFailure MapToFlowFailure(Failure<ProjectSetGetFailureCode> failure)
        =>
        (failure.FailureCode switch
        {
            ProjectSetGetFailureCode.NotAllowed
                => "При поиске проектов произошла ошибка. У вашей учетной записи не достаточно разрешений. Обратитесь к администратору приложения",
            ProjectSetGetFailureCode.TooManyRequests
                => "Слишком много обращений к сервису. Попробуйте повторить попытку через несколько секунд",
            _
                => "При поиске проектов произошла непредвиденная ошибка. Обратитесь к администратору или повторите попытку позднее"
        })
        .Pipe(
            message => BotFlowFailure.From(message, failure.FailureMessage));
}