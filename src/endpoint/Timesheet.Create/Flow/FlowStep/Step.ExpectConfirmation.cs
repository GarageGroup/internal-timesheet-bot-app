using System;
using System.Globalization;
using System.Text.Json;
using System.Web;
using GarageGroup.Infra.Telegram.Bot;
using Microsoft.Extensions.Logging;

namespace GarageGroup.Internal.Timesheet;

using static TimesheetCreateResource;

partial class TimesheetCreateFlowStep
{
    internal static ChatFlow<TimesheetCreateFlowState> ExpectConfirmationOrSkip(
        this ChatFlow<TimesheetCreateFlowState> chatFlow)
        =>
        chatFlow.ExpectConfirmationOrSkip(
            CreateTimesheetConfirmationOption)
        .Forward(
            NextOrRestart)
        .ShowEntityCardOrSkip(
            CreateModificationCardOption);

    private static ConfirmationCardOption<TimesheetCreateFlowState>? CreateTimesheetConfirmationOption(
        IChatFlowContext<TimesheetCreateFlowState> context)
    {
        if (context.FlowState.WithoutConfirmation)
        {
            return null;
        }

        if (context.FlowState.TimesheetId is null)
        {
            return new()
            {
                Entity = context.CreateTimesheetCardOption(context.Localizer[CreationHeaderText]),
                Keyboard = new(
                    confirmButtonText: context.Localizer[CreationConfirmButton],
                    cancelButtonText: context.Localizer[CreationCancelButton],
                    cancelText: context.Localizer[CreationCancelText])
                {
                    WebAppButton = context.BuildEditWebAppButton()
                }
            };
        }

        return new()
        {
            Entity = context.CreateTimesheetCardOption(context.Localizer[UpdateHeaderText]),
            Keyboard = new(
                confirmButtonText: context.Localizer[UpdateConfirmButton],
                cancelButtonText: context.Localizer[UpdateCancelButton],
                cancelText: context.Localizer[UpdateCancelText])
            {
                WebAppButton = context.BuildEditWebAppButton()
            }
        };
    }

    private static CardWebAppButton<TimesheetCreateFlowState>? BuildEditWebAppButton(this IChatFlowContext<TimesheetCreateFlowState> context)
    {
        if (context.WebApp is null)
        {
            return null;
        }

        var timesheet = new WebAppTimesheetCreateData
        {
            Id = context.FlowState.TimesheetId,
            Description = context.FlowState.Description?.Value.OrEmpty(),
            Duration = context.FlowState.Duration.GetValueOrDefault(),
            Project = context.FlowState.Project
        };

        var data = timesheet.CompressDataJson();

        var webAppUrl = context.WebApp.BuildUrl(
            relativePath: "updateTimesheetForm",
            queryParams:
            [
                new("data", HttpUtility.UrlEncode(data)),
                new("date", context.FlowState.Date.GetValueOrDefault().ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)),
                new("days", context.FlowState.LimitationDayOfMonth.ToString(CultureInfo.InvariantCulture))
            ]);

        context.Logger.LogInformation("WebAppUrl: {webAppUrl}", webAppUrl);
        return new(context.Localizer[WebAppEditButton], webAppUrl, context.ParseWebAppTimesheetOrRepeat);
    }

    private static Result<TimesheetCreateFlowState, ChatRepeatState> ParseWebAppTimesheetOrRepeat(
        this IChatFlowContext<TimesheetCreateFlowState> context, BotWebAppData webAppData)
    {
        if (string.IsNullOrWhiteSpace(webAppData.Data))
        {
            return default;
        }

        var timesheet = JsonSerializer.Deserialize<WebAppTimesheetCreateData>(webAppData.Data, SerializerOptions);
        if (timesheet is null)
        {
            return default;
        }

        return context.FlowState with
        {
            Project = timesheet.Project,
            Description = new(timesheet.Description),
            Duration = timesheet.Duration,
            Date = timesheet.Date,
            WithoutConfirmation = true
        };
    }

    private static ChatFlowJump<TimesheetCreateFlowState> NextOrRestart(IChatFlowContext<TimesheetCreateFlowState> context)
        =>
        context.FlowState.Project switch
        {
            null => ChatFlowJump.Restart(context.FlowState with
            {
                WithoutConfirmation = false
            }),
            _ => ChatFlowJump.Next(context.FlowState)
        };

    private static EntityCardOption? CreateModificationCardOption(IChatFlowContext<TimesheetCreateFlowState> context)
    {
        if (context.FlowState.WithoutConfirmation  is false)
        {
            return null;
        }

        return context.CreateTimesheetCardOption(context.Localizer[ModificationHeaderText]);
    }

    private static EntityCardOption CreateTimesheetCardOption(
        this IChatFlowContext<TimesheetCreateFlowState> context, string headerText)
    {
        var projectTypeName = context.FlowState.Project?.TypeDisplayName;
        var dateText = context.FlowState.Date.GetValueOrDefault().ToString(DateFormat, context.User.Culture);
        var durationText = context.FlowState.Duration.GetValueOrDefault().ToString("G", CultureInfo.InvariantCulture);

        return new(headerText)
        {
            FieldValues =
            [
                new(projectTypeName.OrEmpty(), context.FlowState.Project?.Name),
                new(context.Localizer[DateFieldName], dateText),
                new(context.Localizer[DurationFieldName], durationText + context.Localizer[HourSymbol]),
                new(string.Empty, context.FlowState.Description?.Value)
            ]
        };
    }
}