using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace GarageGroup.Internal.Timesheet;

internal sealed record class AgentTimesheetLabels(string Total, string PeriodTotal, string HoursUnit);

// The model describes time entries as data in a <timesheets> block, and this class turns the block into a table.
// So the layout, the totals, the encoding and the splitting of a long report do not depend on the model:
//
// <timesheets from="2026-11-23" to="2026-11-29">
//   <day date="2026-11-27">
//     <entry hours="1.5" type="Проект"><project>E2E Role test</project><comment>#Test</comment></entry>
//   </day>
// </timesheets>
//
// With the 'from' and 'to' attributes the block is a report for a period, without them it is a list of one day
internal static partial class AgentTimesheetHtml
{
    // Telegram accepts 500 blocks and 32768 characters in a rich message; a table row is a block.
    // The limits here are lower, so there is room for the text around the table
    internal const int MaxRowsPerMessage = 200;

    internal const int MaxLengthPerMessage = 20000;

    private const string NonBreakingSpace = " ";

    // Returns the answer with every block replaced by a table. A long report is returned as several messages
    internal static IReadOnlyList<string> Expand(string text, AgentTimesheetLabels labels, string? locale)
    {
        var messages = new List<string>();
        var current = new StringBuilder();
        var currentIndex = 0;

        foreach (Match match in BlockRegex().Matches(text))
        {
            _ = current.Append(text, currentIndex, match.Index - currentIndex);
            currentIndex = match.Index + match.Length;

            var tables = Render(match, labels, locale);
            for (var index = 0; index < tables.Count; index++)
            {
                if (index > 0)
                {
                    messages.Add(current.ToString());
                    _ = current.Clear();
                }

                _ = current.Append(tables[index]);
            }
        }

        _ = current.Append(text, currentIndex, text.Length - currentIndex);
        if (string.IsNullOrWhiteSpace(current.ToString()) is false || messages.Count is 0)
        {
            messages.Add(current.ToString());
        }

        return messages;
    }

    private static List<string> Render(Match block, AgentTimesheetLabels labels, string? locale)
    {
        var days = ReadDays(block.Groups["body"].Value);
        if (days.Count is 0)
        {
            return [];
        }

        var from = ReadDate(FromRegex().Match(block.Groups["attributes"].Value));
        var to = ReadDate(ToRegex().Match(block.Groups["attributes"].Value));
        var isPeriod = days.Count > 1 || (from is not null && to is not null);

        var heading = isPeriod switch
        {
            false => Paragraph(AgentDateFormat.Format(days[0].Date, locale)),
            _ when from is not null && to is not null => Paragraph(AgentDateFormat.FormatPeriod(from.Value, to.Value)),
            _ => string.Empty
        };

        var totalRow = CreateTotalRow(
            isPeriod ? labels.PeriodTotal : labels.Total, days.Sum(static day => day.Total), labels);

        var tables = new List<string>();
        var table = new StringBuilder(heading).Append(TableOpen);
        var rowCount = 0;

        foreach (var day in days)
        {
            var dayRow = isPeriod ? CreateDayRow(day, labels, locale) : null;
            var isDayRowPending = dayRow is not null;

            foreach (var entry in day.Entries)
            {
                var entryRow = CreateEntryRow(entry, labels, areHoursBold: isPeriod is false);
                var length = entryRow.Length + (isDayRowPending ? dayRow!.Length : 0) + totalRow.Length + TableClose.Length;

                if (rowCount > 0 && (rowCount + 3 > MaxRowsPerMessage || table.Length + length > MaxLengthPerMessage))
                {
                    tables.Add(table.Append(TableClose).ToString());
                    table = new StringBuilder(TableOpen);
                    rowCount = 0;

                    // The rest of the day goes to the next message, so the day heading is repeated there
                    isDayRowPending = dayRow is not null;
                }

                if (isDayRowPending)
                {
                    _ = table.Append(dayRow);
                    rowCount++;
                    isDayRowPending = false;
                }

                _ = table.Append(entryRow);
                rowCount++;
            }
        }

        tables.Add(table.Append(totalRow).Append(TableClose).ToString());
        return tables;
    }

    private const string TableOpen = "<table bordered compact>";

    private const string TableClose = "</table>";

    // The shared row of a time entry: the type and the name on one line, the comment on the next one, the hours on the right
    internal static string CreateEntryRow(AgentTimesheetEntry entry, AgentTimesheetLabels labels, bool areHoursBold)
    {
        var builder = new StringBuilder("<tr><td>");
        var hasContent = false;

        if (entry.Type.Length > 0)
        {
            _ = builder.Append("<i>").Append(Encode(entry.Type)).Append("</i>");
            hasContent = true;
        }

        if (entry.Project.Length > 0)
        {
            _ = builder.Append(hasContent ? " " : string.Empty).Append("<b>").Append(Encode(entry.Project)).Append("</b>");
            hasContent = true;
        }

        // An absent comment takes no line
        if (entry.Comment.Length > 0)
        {
            _ = builder.Append(hasContent ? "<br>" : string.Empty).Append(Encode(entry.Comment));
        }

        var hours = Encode(entry.Hours is null ? entry.HoursText : FormatHours(entry.Hours.Value, labels));

        return builder
            .Append("</td><td align=\"right\">")
            .Append(areHoursBold ? "<b>" : string.Empty)
            .Append(hours)
            .Append(areHoursBold ? "</b>" : string.Empty)
            .Append("</td></tr>")
            .ToString();
    }

    // The shared row of totals: of a day list or of the whole period
    internal static string CreateTotalRow(string label, decimal hours, AgentTimesheetLabels labels)
        =>
        $"<tr><td><b>{Encode(label)}</b></td><td align=\"right\"><b>{Encode(FormatHours(hours, labels))}</b></td></tr>";

    // Header cells are the native way to set a row apart from the regular ones
    private static string CreateDayRow(AgentTimesheetDay day, AgentTimesheetLabels labels, string? locale)
        =>
        $"<tr><th align=\"left\">{Encode(AgentDateFormat.FormatDay(day.Date, locale))}</th>" +
        $"<th align=\"right\">{Encode(FormatHours(day.Total, labels))}</th></tr>";

    // The number and the unit are joined by a non-breaking space, so they are never wrapped apart
    internal static string FormatHours(decimal hours, AgentTimesheetLabels labels)
        =>
        hours.ToString("0.##", CultureInfo.InvariantCulture) + NonBreakingSpace + labels.HoursUnit;

    private static string Paragraph(string text)
        =>
        "<p>" + Encode(text) + "</p>";

    private static List<AgentTimesheetDay> ReadDays(string body)
    {
        var days = new List<AgentTimesheetDay>();

        foreach (Match dayMatch in DayRegex().Matches(body))
        {
            if (DateOnly.TryParseExact(
                dayMatch.Groups["date"].Value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date) is false)
            {
                continue;
            }

            var entries = new List<AgentTimesheetEntry>();
            foreach (Match entryMatch in EntryRegex().Matches(dayMatch.Groups["body"].Value))
            {
                var attributes = entryMatch.Groups["attributes"].Value;
                var content = entryMatch.Groups["body"].Value;

                var hoursText = HoursRegex().Match(attributes).Groups["value"].Value.Trim();
                var project = ProjectRegex().Match(content);
                var comment = CommentRegex().Match(content);

                entries.Add(
                    new(
                        Type: Decode(TypeRegex().Match(attributes).Groups["value"].Value),
                        Project: Decode(project.Success ? project.Groups["value"].Value : string.Empty),
                        Comment: Decode(comment.Success ? comment.Groups["value"].Value : string.Empty),
                        HoursText: hoursText,
                        Hours: decimal.TryParse(
                            hoursText.Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture, out var hours) ? hours : null));
            }

            if (entries.Count > 0)
            {
                days.Add(new(date, entries));
            }
        }

        // Days are shown in the calendar order whatever the order of the model is
        return [.. days.OrderBy(static day => day.Date)];
    }

    private static DateOnly? ReadDate(Match match)
        =>
        match.Success && DateOnly.TryParseExact(
            match.Groups["value"].Value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)
            ? date
            : null;

    // The values are plain text: tags the model may have put inside are removed, entities are decoded once
    private static string Decode(string value)
        =>
        WebUtility.HtmlDecode(InnerTagRegex().Replace(value, string.Empty)).Trim();

    private static string Encode(string value)
        =>
        LineBreakRegex().Replace(AgentRichHtml.Encode(value), "<br>");

    internal sealed record class AgentTimesheetEntry(string Type, string Project, string Comment, string HoursText, decimal? Hours);

    private sealed record class AgentTimesheetDay(DateOnly Date, List<AgentTimesheetEntry> Entries)
    {
        public decimal Total { get; } = Entries.Sum(static entry => entry.Hours ?? 0);
    }

    private const RegexOptions Options = RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Singleline;

    [GeneratedRegex(@"<timesheets(?<attributes>[^>]*)>(?<body>.*?)</timesheets>", Options)]
    private static partial Regex BlockRegex();

    [GeneratedRegex("""<day\s+date="(?<date>[^"]*)"[^>]*>(?<body>.*?)</day>""", Options)]
    private static partial Regex DayRegex();

    [GeneratedRegex(@"<entry(?<attributes>[^>]*)>(?<body>.*?)</entry>", Options)]
    private static partial Regex EntryRegex();

    [GeneratedRegex(@"<project>(?<value>.*?)</project>", Options)]
    private static partial Regex ProjectRegex();

    [GeneratedRegex(@"<comment>(?<value>.*?)</comment>", Options)]
    private static partial Regex CommentRegex();

    [GeneratedRegex("""\bfrom="(?<value>[^"]*)" """, Options | RegexOptions.IgnorePatternWhitespace)]
    private static partial Regex FromRegex();

    [GeneratedRegex("""\bto="(?<value>[^"]*)" """, Options | RegexOptions.IgnorePatternWhitespace)]
    private static partial Regex ToRegex();

    [GeneratedRegex("""\bhours="(?<value>[^"]*)" """, Options | RegexOptions.IgnorePatternWhitespace)]
    private static partial Regex HoursRegex();

    [GeneratedRegex("""\btype="(?<value>[^"]*)" """, Options | RegexOptions.IgnorePatternWhitespace)]
    private static partial Regex TypeRegex();

    [GeneratedRegex(@"</?[A-Za-z][^<>]*>", RegexOptions.CultureInvariant)]
    private static partial Regex InnerTagRegex();

    [GeneratedRegex(@"\s*\r?\n\s*", RegexOptions.CultureInvariant)]
    private static partial Regex LineBreakRegex();
}
