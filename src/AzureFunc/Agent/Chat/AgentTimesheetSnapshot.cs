using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;

namespace GarageGroup.Internal.Timesheet;

internal sealed record class AgentTimesheetSnapshotEntry(string Hours, string Type, string Project, string Comment);

// Time entries that the tools have returned during one model turn, by days.
// A day is known when a tool was asked for a period that contains it: a known day without entries is an empty day
internal sealed class AgentTimesheetSnapshot
{
    private const int MaxPeriodDays = 366;

    private const string DateFormat = "yyyy-MM-dd";

    private readonly Dictionary<DateOnly, List<AgentTimesheetSnapshotEntry>> days = [];

    internal bool IsEmpty
    {
        get
        {
            lock (days)
            {
                return days.Count is 0;
            }
        }
    }

    // Remembers the result of a tool that returns time entries of a period: the 'dateFrom' and 'dateTo' arguments
    // and the 'timesheets' array in the result. Any other tool call is ignored
    internal void Remember(IEnumerable<KeyValuePair<string, object?>>? arguments, string? resultJson)
    {
        if (arguments is null || string.IsNullOrWhiteSpace(resultJson))
        {
            return;
        }

        DateOnly? from = null, to = null;
        foreach (var argument in arguments)
        {
            if (string.Equals(argument.Key, "dateFrom", StringComparison.OrdinalIgnoreCase))
            {
                from = ParseDate(argument.Value?.ToString());
            }
            else if (string.Equals(argument.Key, "dateTo", StringComparison.OrdinalIgnoreCase))
            {
                to = ParseDate(argument.Value?.ToString());
            }
        }

        if (from is null || to is null || to < from || to.Value.DayNumber - from.Value.DayNumber >= MaxPeriodDays)
        {
            return;
        }

        var entries = new Dictionary<DateOnly, List<AgentTimesheetSnapshotEntry>>();
        try
        {
            using var document = JsonDocument.Parse(resultJson);

            if (document.RootElement.ValueKind is not JsonValueKind.Object ||
                document.RootElement.TryGetProperty("timesheets", out var timesheets) is false ||
                timesheets.ValueKind is not JsonValueKind.Array)
            {
                return;
            }

            foreach (var item in timesheets.EnumerateArray())
            {
                if (item.ValueKind is not JsonValueKind.Object || ParseDate(GetText(item, "date")) is not DateOnly date)
                {
                    // The result has an unexpected shape, so it cannot be trusted as the full list of the period
                    return;
                }

                if (entries.TryGetValue(date, out var dayEntries) is false)
                {
                    entries[date] = dayEntries = [];
                }

                dayEntries.Add(
                    new(
                        Hours: GetText(item, "duration"),
                        Type: GetText(item, "projectType"),
                        Project: GetText(item, "projectName"),
                        Comment: GetText(item, "description")));
            }
        }
        catch (JsonException)
        {
            return;
        }

        lock (days)
        {
            // The latest result wins: the entries may have been changed between the calls
            for (var date = from.Value; date <= to.Value; date = date.AddDays(1))
            {
                days[date] = entries.TryGetValue(date, out var dayEntries) ? dayEntries : [];
            }
        }
    }

    // Returns null if no tool has returned the entries of the day
    internal IReadOnlyList<AgentTimesheetSnapshotEntry>? Find(DateOnly date)
    {
        lock (days)
        {
            return days.TryGetValue(date, out var entries) ? entries : null;
        }
    }

    internal IReadOnlyList<DateOnly> GetDaysWithEntries(DateOnly from, DateOnly to)
    {
        lock (days)
        {
            return [.. days.Where(day => day.Key >= from && day.Key <= to && day.Value.Count > 0).Select(static day => day.Key)];
        }
    }

    private static DateOnly? ParseDate(string? value)
    {
        // A date may come with a time part: 2026-11-27T00:00:00
        var text = value?.Trim() ?? string.Empty;
        if (text.Length > DateFormat.Length)
        {
            text = text[..DateFormat.Length];
        }

        return DateOnly.TryParseExact(text, DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date) ? date : null;
    }

    private static string GetText(JsonElement element, string field)
    {
        if (element.TryGetProperty(field, out var value) is false)
        {
            return string.Empty;
        }

        return value.ValueKind switch
        {
            JsonValueKind.String => value.GetString().OrEmpty().Trim(),
            JsonValueKind.Number => value.GetDecimal().ToString("0.##", CultureInfo.InvariantCulture),
            _ => string.Empty
        };
    }
}
