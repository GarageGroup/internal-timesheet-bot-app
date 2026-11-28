using System;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Localization;

namespace GarageGroup.Internal.Timesheet;

using static AgentToolActionResource;

// Builds the confirmation message of a prepared tool call from its captured arguments, not from the model text
internal static partial class AgentToolActionPreview
{
    private const string DurationArgumentName = "duration";

    internal static (string RichHtml, string FallbackHtml) Build(AgentPendingAction action, IStringLocalizer localizer, string locale)
    {
        var hint = WebUtility.HtmlEncode(localizer[action.Calls.Length > 1 ? PreviewHintMany : PreviewHint]);
        var rich = new StringBuilder();
        var fallback = new StringBuilder();

        // Several actions get a common heading with their number, and every action is numbered
        if (action.Calls.Length > 1)
        {
            var heading = WebUtility.HtmlEncode(string.Format(CultureInfo.CurrentCulture, localizer[PreviewTitleMany], action.Calls.Length));

            _ = rich.Append("<h3>").Append(heading).Append("</h3>");
            _ = fallback.Append("<b>").Append(heading).Append("</b>\n\n");
        }

        for (var index = 0; index < action.Calls.Length; index++)
        {
            var call = action.Calls[index];
            var number = action.Calls.Length > 1 ? $"{index + 1}. " : string.Empty;
            var title = WebUtility.HtmlEncode(number + GetTitle(call, localizer));

            _ = rich.Append("<p><b>").Append(title).Append("</b></p>");
            _ = fallback.Append(index > 0 ? "\n\n" : string.Empty).Append("<b>").Append(title).Append("</b>");

            if (call.Arguments.Length is 0)
            {
                continue;
            }

            _ = rich.Append("<table compact>");
            _ = fallback.Append('\n');

            foreach (var row in GetRows(call, localizer, locale))
            {
                var label = WebUtility.HtmlEncode(row.Label);
                var value = WebUtility.HtmlEncode(row.Value);

                // A changed value is shown as the struck out old one with the new one under it
                var oldLabel = row.OldLabel is null ? null : "<s>" + WebUtility.HtmlEncode(row.OldLabel) + "</s>";
                var oldValue = row.OldValue is null ? null : "<s>" + WebUtility.HtmlEncode(row.OldValue) + "</s>";

                _ = rich
                    .Append("<tr><th align=\"left\">").Append(oldLabel is null ? string.Empty : oldLabel + "<br>").Append(label)
                    .Append("</th><td>").Append(oldValue is null ? string.Empty : oldValue + "<br>").Append(value).Append("</td></tr>");

                _ = fallback
                    .Append("\n<b>").Append(oldLabel is null ? string.Empty : oldLabel + " ").Append(label).Append(":</b> ")
                    .Append(oldValue is null ? string.Empty : oldValue + " → ").Append(value);
            }

            _ = rich.Append("</table>");
        }

        return (
            rich.Append("<p>").Append(hint).Append("</p>").ToString(),
            fallback.Append("\n\n").Append(hint).ToString());
    }

    // A row of the table. The old label and the old value are set only where an update changes them
    private sealed record class PreviewRow(string Name, string Label, string Value, string RawValue)
    {
        public string? OldLabel { get; init; }

        public string? OldValue { get; init; }
    }

    private static IReadOnlyList<PreviewRow> GetRows(AgentActionCall action, IStringLocalizer localizer, string locale)
    {
        // An action with a single reference to an object, such as deleting a time entry, shows the fields of that object.
        // So the user sees the same table as when the object is created, not one line with everything in it
        if (action.Arguments is [{ Details.Length: > 0 } single])
        {
            return BuildRows(action, single.Details, localizer, locale);
        }

        // An update refers to an object and has new values of its fields: the object is shown with the same table,
        // and a field that gets another value shows both of them
        var references = Array.FindAll(action.Arguments, static argument => argument.Details is { Length: > 0 });
        if (references is not [var reference])
        {
            return BuildRows(action, action.Arguments, localizer, locale);
        }

        var newRows = BuildRows(action, [.. action.Arguments.Where(argument => argument != reference)], localizer, locale);
        var rows = new List<PreviewRow>();

        foreach (var oldRow in BuildRows(action, reference.Details!, localizer, locale))
        {
            var newRow = newRows.FirstOrDefault(row => string.Equals(row.Name, oldRow.Name, StringComparison.Ordinal));
            if (newRow is null)
            {
                rows.Add(oldRow);
                continue;
            }

            // The same object under another spelling of its identifier is not a change
            var isValueChanged =
                string.Equals(oldRow.RawValue, newRow.RawValue, StringComparison.OrdinalIgnoreCase) is false &&
                string.Equals(oldRow.Value, newRow.Value, StringComparison.Ordinal) is false;

            rows.Add(
                (isValueChanged ? newRow : oldRow with { Label = newRow.Label }) with
                {
                    OldLabel = string.Equals(oldRow.Label, newRow.Label, StringComparison.Ordinal) ? null : oldRow.Label,
                    OldValue = isValueChanged ? oldRow.Value : null
                });
        }

        rows.AddRange(newRows.Where(newRow => rows.TrueForAll(row => string.Equals(row.Name, newRow.Name, StringComparison.Ordinal) is false)));
        return rows;
    }

    // An argument can take its label from the value of another argument, which is then not shown as a separate row.
    // This is how a project is shown as one row "<type> | <name>" instead of two rows "Project" and "Type"
    private static List<PreviewRow> BuildRows(
        AgentActionCall action, AgentActionArgument[] arguments, IStringLocalizer localizer, string locale)
    {
        var labelSources = new Dictionary<string, AgentActionArgument>(StringComparer.Ordinal);

        foreach (var argument in arguments)
        {
            var sourceName = FindForArgument(localizer, LabelFromPrefix, action, argument);
            var source = sourceName is null ? null : Array.Find(arguments, item => string.Equals(item.Name, sourceName, StringComparison.Ordinal));

            if (source is not null)
            {
                labelSources[argument.Name] = source;
            }
        }

        var rows = new List<PreviewRow>();
        foreach (var argument in arguments)
        {
            if (labelSources.Values.Contains(argument))
            {
                continue;
            }

            var label = labelSources.TryGetValue(argument.Name, out var labelSource)
                ? GetValue(labelSource, localizer, locale)
                : GetLabel(action, argument, localizer);

            rows.Add(new(argument.Name, label, GetValue(argument, localizer, locale), argument.Value));
        }

        return rows;
    }

    internal static string GetTitle(AgentActionCall action, IStringLocalizer localizer)
        =>
        Find(localizer, TitlePrefix + action.FunctionName) ?? string.Format(CultureInfo.CurrentCulture, localizer[DefaultTitle], action.FunctionName);

    private static string GetLabel(AgentActionCall action, AgentActionArgument argument, IStringLocalizer localizer)
        =>
        FindForArgument(localizer, ArgumentPrefix, action, argument) ?? argument.Name;

    // A resource for an argument of the operation wins over a resource for the argument name in general
    private static string? FindForArgument(IStringLocalizer localizer, string prefix, AgentActionCall action, AgentActionArgument argument)
        =>
        Find(localizer, $"{prefix}{action.FunctionName}_{argument.Name}") ?? Find(localizer, prefix + argument.Name);

    private static string GetValue(AgentActionArgument argument, IStringLocalizer localizer, string locale)
    {
        // The unit of a duration is a part of the value, not of the label: "2 h"
        if (argument.Name is DurationArgumentName &&
            decimal.TryParse(argument.Value, NumberStyles.Number, CultureInfo.InvariantCulture, out var hours))
        {
            var unit = Find(localizer, HoursUnit);
            return hours.ToString("0.##", CultureInfo.InvariantCulture) + (unit is null ? string.Empty : "\u00A0" + unit);
        }

        // A project type is shown with its icon: next to the name of the type or, in front of a project name, instead of it
        var value = string.IsNullOrWhiteSpace(argument.Display)
            ? JoinIcon(AgentProjectTypeIcon.Find(argument.Value), Find(localizer, ValuePrefix + argument.Value) ?? argument.Value)
            : AgentEntityCache.ValueTokenRegex().Replace(
                argument.Display,
                match => AgentProjectTypeIcon.Find(match.Groups[1].Value)
                    ?? Find(localizer, ValuePrefix + match.Groups[1].Value)
                    ?? match.Groups[1].Value);

        // Dates arrive in the API format, both as argument values and inside the names resolved from tool results
        return IsoDateRegex().Replace(
            value,
            match => DateOnly.TryParseExact(match.Value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)
                ? AgentDateFormat.Format(date, locale)
                : match.Value);
    }

    private static string JoinIcon(string? icon, string text)
        =>
        icon is null ? text : icon + " " + text;

    [GeneratedRegex(@"(?<!\d)\d{4}-\d{2}-\d{2}(?!\d)", RegexOptions.CultureInvariant)]
    private static partial Regex IsoDateRegex();

    private static string? Find(IStringLocalizer localizer, string name)
    {
        var resource = localizer[name];
        return resource.ResourceNotFound ? null : resource.Value;
    }
}
