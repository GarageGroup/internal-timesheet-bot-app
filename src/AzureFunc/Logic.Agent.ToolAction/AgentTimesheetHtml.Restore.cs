using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace GarageGroup.Internal.Timesheet;

partial class AgentTimesheetHtml
{
    // The model copies time entries from a tool result to the block and may lose some of them in a long list.
    // So the entries of every day that a tool has returned in this turn are taken from the tool result:
    // the model only chooses which days to show. A block with the 'partial' attribute is a selection made
    // on the user's request (one project, one tag), so it is left as the model has written it
    internal static string Restore(string text, AgentTimesheetSnapshot snapshot)
        =>
        snapshot.IsEmpty ? text : BlockRegex().Replace(text, match => RestoreBlock(match, snapshot));

    private static string RestoreBlock(Match block, AgentTimesheetSnapshot snapshot)
    {
        var attributes = block.Groups["attributes"].Value;
        if (PartialRegex().IsMatch(attributes))
        {
            return block.Value;
        }

        var modelDays = new SortedDictionary<DateOnly, string>();
        foreach (Match dayMatch in DayRegex().Matches(block.Groups["body"].Value))
        {
            if (DateOnly.TryParseExact(dayMatch.Groups["date"].Value, "yyyy-MM-dd", out var date))
            {
                // Entries of a day that the model has split into several blocks are kept together
                modelDays[date] = modelDays.TryGetValue(date, out var dayText) ? dayText + dayMatch.Value : dayMatch.Value;
            }
        }

        var dates = new SortedSet<DateOnly>(modelDays.Keys);
        if (ReadDate(FromRegex().Match(attributes)) is DateOnly from && ReadDate(ToRegex().Match(attributes)) is DateOnly to)
        {
            // A day of the period that the model has missed is shown too
            dates.UnionWith(snapshot.GetDaysWithEntries(from, to));
        }

        if (dates.Any(date => snapshot.Find(date) is not null) is false)
        {
            return block.Value;
        }

        var builder = new StringBuilder("<timesheets").Append(attributes).Append('>');
        var dayCount = 0;

        foreach (var date in dates)
        {
            var entries = snapshot.Find(date);
            if (entries is null)
            {
                _ = builder.Append('\n').Append(modelDays[date]);
                dayCount++;
                continue;
            }

            if (entries.Count is 0)
            {
                continue;
            }

            _ = builder.Append("\n<day date=\"").Append(date.ToString("yyyy-MM-dd")).Append("\">");
            foreach (var entry in entries)
            {
                _ = builder
                    .Append("\n<entry hours=\"").Append(WebUtility.HtmlEncode(entry.Hours))
                    .Append("\" type=\"").Append(WebUtility.HtmlEncode(entry.Type)).Append("\">")
                    .Append("<project>").Append(WebUtility.HtmlEncode(entry.Project)).Append("</project>");

                if (entry.Comment.Length > 0)
                {
                    _ = builder.Append("<comment>").Append(WebUtility.HtmlEncode(entry.Comment)).Append("</comment>");
                }

                _ = builder.Append("</entry>");
            }

            _ = builder.Append("\n</day>");
            dayCount++;
        }

        // The tools say that the days of the block have no entries: the model text is kept, it is not replaced by nothing
        return dayCount is 0 ? block.Value : builder.Append("\n</timesheets>").ToString();
    }

    [GeneratedRegex(@"(?<![\w""=-])partial(?![\w-])", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex PartialRegex();
}
