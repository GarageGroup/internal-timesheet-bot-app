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
    internal static (string RichHtml, string FallbackHtml) Build(AgentPendingAction action, IStringLocalizer localizer, string locale)
    {
        var title = WebUtility.HtmlEncode(GetTitle(action, localizer));
        var hint = WebUtility.HtmlEncode(localizer[PreviewHint]);

        var rich = new StringBuilder("<p><b>").Append(title).Append("</b></p>");
        var fallback = new StringBuilder("<b>").Append(title).Append("</b>");

        if (action.Arguments.Length > 0)
        {
            _ = rich.Append("<table compact>");
            _ = fallback.Append('\n');

            foreach (var (labelText, valueText) in GetRows(action, localizer, locale))
            {
                var label = WebUtility.HtmlEncode(labelText);
                var value = WebUtility.HtmlEncode(valueText);

                _ = rich.Append("<tr><th align=\"left\">").Append(label).Append("</th><td>").Append(value).Append("</td></tr>");
                _ = fallback.Append("\n<b>").Append(label).Append(":</b> ").Append(value);
            }

            _ = rich.Append("</table>");
        }

        return (
            rich.Append("<p>").Append(hint).Append("</p>").ToString(),
            fallback.Append("\n\n").Append(hint).ToString());
    }

    // An argument can take its label from the value of another argument, which is then not shown as a separate row.
    // This is how a project is shown as one row "<type> | <name>" instead of two rows "Project" and "Type"
    private static IEnumerable<(string Label, string Value)> GetRows(AgentPendingAction action, IStringLocalizer localizer, string locale)
    {
        // An action with a single reference to an object, such as deleting a time entry, shows the fields of that object.
        // So the user sees the same table as when the object is created, not one line with everything in it
        var arguments = action.Arguments is [{ Details.Length: > 0 } reference] ? reference.Details : action.Arguments;
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

        foreach (var argument in arguments)
        {
            if (labelSources.Values.Contains(argument))
            {
                continue;
            }

            var label = labelSources.TryGetValue(argument.Name, out var labelSource)
                ? GetValue(labelSource, localizer, locale)
                : GetLabel(action, argument, localizer);

            yield return (label, GetValue(argument, localizer, locale));
        }
    }

    internal static string GetTitle(AgentPendingAction action, IStringLocalizer localizer)
        =>
        Find(localizer, TitlePrefix + action.FunctionName) ?? string.Format(CultureInfo.CurrentCulture, localizer[DefaultTitle], action.FunctionName);

    private static string GetLabel(AgentPendingAction action, AgentActionArgument argument, IStringLocalizer localizer)
        =>
        FindForArgument(localizer, ArgumentPrefix, action, argument) ?? argument.Name;

    // A resource for an argument of the operation wins over a resource for the argument name in general
    private static string? FindForArgument(IStringLocalizer localizer, string prefix, AgentPendingAction action, AgentActionArgument argument)
        =>
        Find(localizer, $"{prefix}{action.FunctionName}_{argument.Name}") ?? Find(localizer, prefix + argument.Name);

    private static string GetValue(AgentActionArgument argument, IStringLocalizer localizer, string locale)
    {
        var value = string.IsNullOrWhiteSpace(argument.Display)
            ? Find(localizer, ValuePrefix + argument.Value) ?? argument.Value
            : AgentEntityCache.ValueTokenRegex().Replace(
                argument.Display, match => Find(localizer, ValuePrefix + match.Groups[1].Value) ?? match.Groups[1].Value);

        // Dates arrive in the API format, both as argument values and inside the names resolved from tool results
        return IsoDateRegex().Replace(
            value,
            match => DateOnly.TryParseExact(match.Value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)
                ? AgentDateFormat.Format(date, locale)
                : match.Value);
    }

    [GeneratedRegex(@"(?<!\d)\d{4}-\d{2}-\d{2}(?!\d)", RegexOptions.CultureInvariant)]
    private static partial Regex IsoDateRegex();

    private static string? Find(IStringLocalizer localizer, string name)
    {
        var resource = localizer[name];
        return resource.ResourceNotFound ? null : resource.Value;
    }
}
