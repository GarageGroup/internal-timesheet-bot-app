using GarageGroup.Infra.Bot.Builder;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace GarageGroup.Internal.Timesheet;

partial class TimesheetDeleteFlowStep
{
    internal static ChatFlow<DeleteTimesheetFlowState> AwaitDateWebApp(this ChatFlow<DeleteTimesheetFlowState> chatFlow)
        =>
        chatFlow.ForwardValue(SelectDate);

    private static async ValueTask<ChatFlowJump<DeleteTimesheetFlowState>> SelectDate(
        IChatFlowContext<DeleteTimesheetFlowState> context, CancellationToken token)
    {
        var turnContext = (ITurnContext)context;

        if (context.StepState is DateWebAppCacheJson cache)
        {
            return await AsyncPipeline
                .Pipe(
                    context, token)
                .Pipe(
                    _ => ParseDate(context, cache))
                .MapSuccess(
                    data => (data, context))
                .Forward(
                    ValidateDate)
                .MapSuccess(
                    date => (context, date, cache))
                .FoldValue(
                    SuccessAsync,
                    async (botFailure, token) =>
                    {
                        var activity = MessageFactory.Text(botFailure.UserMessage);
                        await SendInsteadActivityAsync(context, cache.ActivityId, activity, token);
                        return context.RepeatSameStateJump<DeleteTimesheetFlowState>();
                    })
                .ToTask();
        }

        var activity = MessageFactory.Text($"Выбери дату или введите дату в формате {DatePlaceholder}");
        activity.ChannelData = JObject.FromObject(CreateChannelDataSelectDate(context.FlowState.Options.UrlWebApp));

        var resourse = await turnContext.SendActivityAsync(activity, token).ConfigureAwait(false);

        return ChatFlowJump.Repeat<DeleteTimesheetFlowState>(new DateWebAppCacheJson(resourse?.Id));
    }

    private static Result<DateOnly, BotFlowFailure> ParseDate(
        IChatFlowContext<DeleteTimesheetFlowState> context, 
        DateWebAppCacheJson cache)
    {
        var json = JsonConvert.SerializeObject(context.Activity.ChannelData);
        var dataWebApp = JsonConvert.DeserializeObject<WebAppResponseJson>(json);

        if (dataWebApp?.Message?.WebAppData is null)
        {
            return DateOnly.TryParse(context.Activity.Text, out var messageDate) ? messageDate : BotFlowFailure.From("Не удалось распознать дату");
        }

        return DateOnly.TryParse(dataWebApp.Message.WebAppData.Data, out var dateWebAppData) ? dateWebAppData : BotFlowFailure.From("Не удалось распознать дату"); 
    }

    private static Result<DateOnly, BotFlowFailure> ValidateDate(
        (DateOnly Date, IChatFlowContext<DeleteTimesheetFlowState> Context) inputDate)
    {
        if (inputDate.Date < DateOnly.FromDateTime(DateTime.Now.Subtract(inputDate.Context.FlowState.Options.TimesheetInterval)))
        {
            return BotFlowFailure.From($"Дата не входит в допустимый интервал ({inputDate.Context.FlowState.Options.TimesheetInterval.Days} дней)");
        }

        return inputDate.Date;
    }

    private static async ValueTask<ChatFlowJump<DeleteTimesheetFlowState>> SuccessAsync(
        (IChatFlowContext<DeleteTimesheetFlowState> Context, DateOnly Date, DateWebAppCacheJson Cache) input,
        CancellationToken cancellationToken = default)
    {
        var activity = MessageFactory.Text($"Дата: {input.Date}");
        await SendInsteadActivityAsync(input.Context, input.Cache.ActivityId, activity, cancellationToken);
        return input.Context.FlowState with
        {
            Date = input.Date
        };
    }

    private static Task SendInsteadActivityAsync(
        this ITurnContext context, 
        string? activityId, 
        IActivity activity, 
        CancellationToken token)
    {
        return string.IsNullOrEmpty(activityId) || context.IsNotMsteamsChannel()
            ? SendActivityAsync()
            : Task.WhenAll(DeleteActivityAsync(), SendActivityAsync());

        Task SendActivityAsync()
            =>
            context.SendActivityAsync(activity, token);

        Task DeleteActivityAsync()
            =>
            context.DeleteActivityAsync(activityId, token);
    }

    private static WebAppChannelDataJson CreateChannelDataSelectDate(string url)
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
                                Text = "Выбрать дату",
                                WebApp = new WebAppJson
                                {
                                    Url = $"{url}/selectDate"
                                }
                            }
                        ]
                    ],
                    ResizeKeyboar = true,
                    OneTimeKeyboard = true,
                    InputFieldPlaceholder = DatePlaceholder
                }
            }
        };
}