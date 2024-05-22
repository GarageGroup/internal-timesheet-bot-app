using System;
using System.Text.Json;
using GarageGroup.Infra.Telegram.Bot;

namespace GarageGroup.Internal.Timesheet;

partial class TimesheetCreateCommand
{
    public Optional<TimesheetCreateCommandIn> Parse(ChatUpdate update)
    {
        if (string.IsNullOrWhiteSpace(update.Message?.WebAppData?.Data))
        {
            return default;
        }

        var data = JsonSerializer.Deserialize<WebAppTimesheetUpdateData>(update.Message.WebAppData.Data, SerializerOptions);
        if (data is null || string.Equals(data.Command, "updatetimesheet", StringComparison.InvariantCultureIgnoreCase) is false)
        {
            return default;
        }

        return new TimesheetCreateCommandIn
        {
            TimesheetId = data.Id,
            Description = data.Description,
            Duration = data.Duration,
            Project = data.Project is null ? null : new()
            {
                Id = data.Project.Id,
                Name = data.Project.Name,
                Type = data.Project.Type
            },
            Date = data.Date
        };
    }
}