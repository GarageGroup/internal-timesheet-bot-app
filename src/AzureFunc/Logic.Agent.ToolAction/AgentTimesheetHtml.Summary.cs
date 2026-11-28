using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace GarageGroup.Internal.Timesheet;

// A summary of time entries is never calculated by the model: it only asks for one with an empty block,
//
// <summary by="project" from="2026-11-01" to="2026-11-30"></summary>
//
// and the hours are summed here from the time entries that the tools have returned in this turn.
// The 'by' attribute is the grouping: day, project or type
partial class AgentTimesheetHtml
{
    private const string SummaryByDay = "day";

    private const string SummaryByProject = "project";

    private const string SummaryByType = "type";

    // Writes the rows of every summary block. Rows written by the model are dropped: its sums are not trusted.
    // A block of a period that no tool has returned gets no rows, and such a block is not shown
    private static string RestoreSummaries(string text, AgentTimesheetSnapshot snapshot)
        =>
        SummaryRegex().Replace(text, match => RestoreSummary(match, snapshot));

    private static string RestoreSummary(Match block, AgentTimesheetSnapshot snapshot)
    {
        var attributes = block.Groups["attributes"].Value.TrimEnd().TrimEnd('/').TrimEnd();
        var builder = new StringBuilder("<summary").Append(attributes).Append('>');

        var by = ByRegex().Match(attributes).Groups["value"].Value.Trim().ToLowerInvariant();

        if (ReadDate(FromRegex().Match(attributes)) is DateOnly from &&
            ReadDate(ToRegex().Match(attributes)) is DateOnly to &&
            by is SummaryByDay or SummaryByProject or SummaryByType)
        {
            var entries = snapshot.GetDaysWithEntries(from, to)
                .Order()
                .SelectMany(date => (snapshot.Find(date) ?? []).Select(entry => (Date: date, Entry: entry, Hours: ParseHours(entry.Hours))));

            var rows = by switch
            {
                SummaryByDay => entries
                    .GroupBy(static item => item.Date)
                    .Select(static group => (Date: (DateOnly?)group.Key, Type: string.Empty, Name: string.Empty, Hours: group.Sum(static item => item.Hours))),
                SummaryByProject => entries
                    .GroupBy(static item => (item.Entry.Type, item.Entry.Project))
                    .Select(static group => (Date: (DateOnly?)null, group.Key.Type, Name: group.Key.Project, Hours: group.Sum(static item => item.Hours)))
                    .OrderByDescending(static row => row.Hours).ThenBy(static row => row.Name, StringComparer.CurrentCultureIgnoreCase),
                _ => entries
                    .GroupBy(static item => item.Entry.Type)
                    .Select(static group => (Date: (DateOnly?)null, Type: group.Key, Name: string.Empty, Hours: group.Sum(static item => item.Hours)))
                    .OrderByDescending(static row => row.Hours).ThenBy(static row => row.Type, StringComparer.CurrentCultureIgnoreCase)
            };

            foreach (var row in rows)
            {
                _ = builder.Append("\n<row hours=\"").Append(row.Hours.ToString("0.##", CultureInfo.InvariantCulture)).Append('"');

                if (row.Date is not null)
                {
                    _ = builder.Append(" date=\"").Append(row.Date.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)).Append('"');
                }

                if (row.Type.Length > 0)
                {
                    _ = builder.Append(" type=\"").Append(WebUtility.HtmlEncode(row.Type)).Append('"');
                }

                _ = builder.Append('>').Append(WebUtility.HtmlEncode(row.Name)).Append("</row>");
            }
        }

        return builder.Append("\n</summary>").ToString();
    }

    private static string ExpandSummaries(string text, AgentTimesheetLabels labels, string? locale)
        =>
        SummaryRegex().Replace(text, match => RenderSummary(match, labels, locale));

    // The table of a summary: a row for a group with its hours and the total of the period. A block without rows is removed
    private static string RenderSummary(Match block, AgentTimesheetLabels labels, string? locale)
    {
        var builder = new StringBuilder();
        var total = 0m;

        foreach (Match row in SummaryRowRegex().Matches(block.Groups["body"].Value))
        {
            var attributes = row.Groups["attributes"].Value;
            var hours = ParseHours(HoursRegex().Match(attributes).Groups["value"].Value);

            var type = Decode(TypeRegex().Match(attributes).Groups["value"].Value);
            var name = Decode(row.Groups["value"].Value);

            _ = builder.Append("<tr><td>");
            if (ReadDate(DateRegex().Match(attributes)) is DateOnly date)
            {
                _ = builder.Append(Encode(AgentDateFormat.FormatDay(date, locale)));
            }
            else
            {
                if (type.Length > 0)
                {
                    var typeName = labels.LocalizeType?.Invoke(type) ?? type;
                    var icon = AgentProjectTypeIcon.Find(type) ?? AgentProjectTypeIcon.Find(typeName);

                    // A row of a project has only the icon; a row of a type has the name too, as the icon alone says too little
                    _ = builder.Append(
                        icon is null ? "<i>" + Encode(typeName) + "</i>" : name.Length > 0 ? Encode(icon) : Encode(icon + " " + typeName));
                }

                if (name.Length > 0)
                {
                    _ = builder.Append(type.Length > 0 ? " " : string.Empty).Append("<b>").Append(Encode(name)).Append("</b>");
                }
            }

            _ = builder.Append("</td><td align=\"right\">").Append(Encode(FormatHours(hours, labels))).Append("</td></tr>");
            total += hours;
        }

        if (builder.Length is 0)
        {
            return string.Empty;
        }

        var blockAttributes = block.Groups["attributes"].Value;
        var heading = ReadDate(FromRegex().Match(blockAttributes)) is DateOnly from && ReadDate(ToRegex().Match(blockAttributes)) is DateOnly to
            ? Paragraph(AgentDateFormat.FormatPeriod(from, to))
            : string.Empty;

        return heading + TableOpen + builder + CreateTotalRow(labels.PeriodTotal, total, labels) + TableClose;
    }

    private static decimal ParseHours(string text)
        =>
        decimal.TryParse(text.Trim().Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture, out var hours) ? hours : 0;

    [GeneratedRegex(@"<summary(?<attributes>[^>]*?)(?:/>|>(?<body>.*?)</summary>)", Options)]
    private static partial Regex SummaryRegex();

    [GeneratedRegex(@"<row(?<attributes>[^>]*)>(?<value>.*?)</row>", Options)]
    private static partial Regex SummaryRowRegex();

    [GeneratedRegex("""\bby="(?<value>[^"]*)" """, Options | RegexOptions.IgnorePatternWhitespace)]
    private static partial Regex ByRegex();

    [GeneratedRegex("""\bdate="(?<value>[^"]*)" """, Options | RegexOptions.IgnorePatternWhitespace)]
    private static partial Regex DateRegex();
}
