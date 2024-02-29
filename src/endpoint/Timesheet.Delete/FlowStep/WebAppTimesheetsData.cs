using GarageGroup.Internal.Timesheet;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flow.FlowStep
{
    internal sealed record WebAppTimesheetsData
    {
        [JsonProperty("date")]
        public string? Date { get; init; }

        [JsonProperty("timesheets")]
        public IReadOnlyCollection<TimesheetJson>? Timesheets { get; init; }
    }
}
