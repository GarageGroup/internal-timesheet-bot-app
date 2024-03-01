using GarageGroup.Infra.Bot.Builder;
using Microsoft.Bot.Builder;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace GarageGroup.Internal.Timesheet;
partial class TimesheetDeleteFlowStep
{
    internal static ChatFlow<DeleteTimesheetFlowState> AwaitTimesheetWebApp(this ChatFlow<DeleteTimesheetFlowState> chatFlow)
        =>
        chatFlow.ForwardValue(SelectTimesheet);

    private static async ValueTask<ChatFlowJump<DeleteTimesheetFlowState>> SelectTimesheet(
        IChatFlowContext<DeleteTimesheetFlowState> context, CancellationToken token)
    {
        var turnContext = (ITurnContext)context;

        if (context.StepState is DateWebAppCacheJson cache)
        {
            return await AsyncPipeline
                .Pipe(
                    context, token)
                .Pipe(
                    _ => ParseTimesheet(context))
                .MapSuccess(
                    data => (context, data))
                .FoldValue(
                    SuccessSelectTimesheetAsync,
                    async (botFailure, token) =>
                    {
                        var activity = MessageFactory.Text(botFailure.UserMessage);
                        await SendInsteadActivityAsync(context, cache.ActivityId, activity, token);
                        return context.RepeatSameStateJump<DeleteTimesheetFlowState>();
                    })
                .ToTask();
        }

        var activity = MessageFactory.Text("Выбери списания времени, которые нужно удалить");

        var webAppData = new WebAppTimesheetsDataJson
        {
            Date = context.FlowState.Date.ToString(),
            Timesheets = context.FlowState.Timesheets,
        };

        var webAppDataJson = JsonConvert.SerializeObject(webAppData);
        var base64Timesheets = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(webAppDataJson));

        activity.ChannelData = JObject.FromObject(CreateChannelDataSelectTimesheet(context.FlowState.Options.UrlWebApp, base64Timesheets));

        var resourse = await turnContext.SendActivityAsync(activity, token).ConfigureAwait(false);

        return ChatFlowJump.Repeat<DeleteTimesheetFlowState>(new DateWebAppCacheJson(resourse?.Id));
    }

    private static async ValueTask<ChatFlowJump<DeleteTimesheetFlowState>> SuccessSelectTimesheetAsync(
        (IChatFlowContext<DeleteTimesheetFlowState> Context, FlatArray<Guid> TimesheetsId) input, 
        CancellationToken cancellationToken = default)
    {
        await Task.Yield();
        return input.Context.FlowState with
        {
            DeleteTimesheetsId = input.TimesheetsId
        };
    }

    private static Result<FlatArray<Guid>, BotFlowFailure> ParseTimesheet(IChatFlowContext<DeleteTimesheetFlowState> context)
    {
        var json = JsonConvert.SerializeObject(context.Activity.ChannelData);
        var dataWebApp = JsonConvert.DeserializeObject<WebAppResponseJson>(json);

        if (dataWebApp?.Message?.WebAppData is null)
        {
            return BotFlowFailure.From("Выберете списания времени через сайт (кнопка ниже)");
        }

        var timesheets = JsonConvert.DeserializeObject<List<TimesheetJson>>(dataWebApp.Message.WebAppData.Data!);

        if (timesheets != null)
        {
            return new FlatArray<Guid>(timesheets.Select(t => t.Id).ToArray());
        }
        else
        {
            return BotFlowFailure.From("Не удалось получить списания времени!");
        }
    }

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
                                    Url = $"{url}/selectTimesheet?data={data}"
                                }
                            }
                        ]
                    ],
                    ResizeKeyboar = true,
                    InputFieldPlaceholder = "Чтобы выбрать списания, нажмите кнопку"
                }
            }
        };
}