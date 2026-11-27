using System;
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

            foreach (var argument in action.Arguments)
            {
                var label = WebUtility.HtmlEncode(GetLabel(action, argument, localizer));
                var value = WebUtility.HtmlEncode(GetValue(argument, localizer, locale));

                _ = rich.Append("<tr><th align=\"left\">").Append(label).Append("</th><td>").Append(value).Append("</td></tr>");
                _ = fallback.Append("\n<b>").Append(label).Append(":</b> ").Append(value);
            }

            _ = rich.Append("</table>");
        }

        return (
            rich.Append("<p>").Append(hint).Append("</p>").ToString(),
            fallback.Append("\n\n").Append(hint).ToString());
    }

    internal static string GetTitle(AgentPendingAction action, IStringLocalizer localizer)
        =>
        Find(localizer, TitlePrefix + action.FunctionName) ?? string.Format(CultureInfo.CurrentCulture, localizer[DefaultTitle], action.FunctionName);

    private static string GetLabel(AgentPendingAction action, AgentActionArgument argument, IStringLocalizer localizer)
        =>
        Find(localizer, $"{ArgumentPrefix}{action.FunctionName}_{argument.Name}")
        ?? Find(localizer, ArgumentPrefix + argument.Name)
        ?? argument.Name;

    private static string GetValue(AgentActionArgument argument, IStringLocalizer localizer, string locale)
    {
        var value = string.IsNullOrWhiteSpace(argument.Display)
            ? Find(localizer, ValuePrefix + argument.Value) ?? argument.Value
            : argument.Display;

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
