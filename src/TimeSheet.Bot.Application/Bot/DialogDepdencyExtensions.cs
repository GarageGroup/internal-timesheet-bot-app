#nullable enable

using System;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using GGroupp.Infra;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using PrimeFuncPack;

namespace GGroupp.Internal.Timesheet.Bot
{
    internal static class DialogDepdencyExtensions
    {
        private static readonly OAuthPromptSettings oauthSettings = new()
        {
            ConnectionName = "TimesheetOAuthConnection",
            Text = "Войдите в свою учетную запись",
            Title = "Вход",
            Timeout = 300000
        };

        public static Dependency<Dialog> UseTimesheetGetDialog(this Dependency<UserState> dependency)
            =>
            dependency.Map(GetDialog);

        private static Dialog GetDialog(IServiceProvider serviceProvider, UserState userState)
            =>
            FlowDialog.Start<TimesheetGetCommand>(
                dialogId: "TimesheetGet")
            .On(
                (context, token) => OAuthPrompt.SendOAuthCardAsync(oauthSettings, context, null, token))
            .Await()
            .Next(
                async (context, token) =>
                {
                    var result = await OAuthPrompt.RecognizeTokenAsync(oauthSettings, context.DialogContext, token).ConfigureAwait(false);
                    if(result.Succeeded is false)
                    {
                        var retryActivity = MessageFactory.Text("Не удалось авторизавоться. Повторите попытку");
                        await context.SendActivityAsync(retryActivity, token).ConfigureAwait(false);

                        return FlowDialogAction.RetryTurn(context.State);
                    }

                    var userId = await GetUserIdAsync(serviceProvider, result.Value, token).ConfigureAwait(false);
                    if(userId.IsSuccess)
                    {
                        return FlowDialogAction.Next(context.State with { UserId = userId.SuccessOrThrow() });
                    }

                    var failureActivity = MessageFactory.Text(userId.FailureOrThrow().FailureMessage);
                    await context.SendActivityAsync(failureActivity, token).ConfigureAwait(false);

                    return FlowDialogAction.RetryTurn(context.State);
                })
            .SendActivity(
                () => MessageFactory.Text("Введите дату начала периода периода просомотра"))
            .SendActivity(
                context => context.Activity.GetReplyFromCard(GetStartDateOfWeek().Pipe(DateTimeAdaptiveCard.Create)))
            .AwaitForResult(
                context => context.Activity.Value.ToStringOrEmpty().DeserializeOrFailure<CardDateValueJson>().Map(
                    value => context.State with { DateFrom = new(value.Date.Date, default) },
                    _ => MessageFactory.Text("Значение должно быть указано. Повторите попытку...").ToRetryTurnFailure()))
            .SendActivity(
                () => MessageFactory.Text("Введите дату окончания периода просомотра"))
            .SendActivity(
                context => context.Activity.GetReplyFromCard(context.State.DateFrom.AddDays(6).Pipe(DateTimeAdaptiveCard.Create)))
            .AwaitForResult(
                context => context.Activity.Value.ToStringOrEmpty().DeserializeOrFailure<CardDateValueJson>().Map(
                    value => context.State with { DateTo = new(value.Date.Date, default) },
                    _ => MessageFactory.Text("Значение должно быть указано. Повторите попытку...").ToRetryTurnFailure()))
            .SendActivity(
                context => MessageFactory.Text(context.State.ToString()))
            .On(
                (context, token) => userState.CreateProperty<TimesheetGetCommand>("TimesheetGet").SetAsync(context, context.State, token))
            .End();

        private static async ValueTask<Result<Guid, Failure<HttpStatusCode>>> GetUserIdAsync(
            IServiceProvider provider, TokenResponse token, CancellationToken cancellationToken)
        {
            var socketsHandler = provider.GetServiceOrThrow<ISocketsHttpHandlerProvider>().GetOrCreate("CurrentUserData");
            var httpClient = new HttpClient(socketsHandler, disposeHandler: false)
            {
                BaseAddress = new("https://graph.microsoft.com/")
            };
            httpClient.DefaultRequestHeaders.Authorization = new("Bearer", token.Token);
            var response = await httpClient.GetAsync("/v1.0/me?$select=id", cancellationToken).ConfigureAwait(false);

            var json = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            if(response.IsSuccessStatusCode)
            {
                return JsonSerializer.Deserialize<UserDataJson>(json)?.Id ?? default;
            }

            return Failure.Create(response.StatusCode, json);
        }

        private static Result<T, Unit> DeserializeOrFailure<T>([AllowNull] this string json)
            =>
            json.IsNotNullOrEmpty()
                ? JsonSerializer.Deserialize<T>(json)!
                : Result.Absent<T>();

        private static DateTimeOffset GetStartDateOfWeek()
            =>
            (DateTimeOffset.Now, DateTime.Now.DayOfWeek) switch
            {
                (var date, DayOfWeek.Sunday) => date.AddDays(-6),
                (var date, var dayOfWeek) => date.AddDays((int)dayOfWeek * (-1) + 1)
            };
    }
}