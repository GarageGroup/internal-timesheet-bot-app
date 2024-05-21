using System;
using System.Text.Json;
using GarageGroup.Infra.Telegram.Bot;

namespace GarageGroup.Internal.Timesheet;

partial class TimesheetDeleteCommand
{
    public Optional<TimesheetDeleteCommandIn> Parse(ChatUpdate update)
    {
        if (string.IsNullOrWhiteSpace(update.Message?.WebAppData?.Data))
        {
            return default;
        }

        var data = JsonSerializer.Deserialize<WebAppTimesheetDeleteData>(update.Message.WebAppData.Data, SerializerOptions);
        if (data?.Timesheet is null || string.Equals(data.Command, "deletetimesheet", StringComparison.InvariantCultureIgnoreCase) is false)
        {
            return default;
        }

        return new TimesheetDeleteCommandIn
        {
            TimesheetId = data.Timesheet.Id,
            Date = data.Date,
            Description = data.Timesheet.Description,
            Duration = data.Timesheet.Duration,
            ProjectName = data.Timesheet.Project?.Name,
            ProjectTypeDisplayName = data.Timesheet.Project?.TypeDisplayName,
        };
    }
}
