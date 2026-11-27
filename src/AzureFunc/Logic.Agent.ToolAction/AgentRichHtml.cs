using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace GarageGroup.Internal.Timesheet;

// Prepares a model answer for Telegram. The model may use a fixed set of tags; everything else is shown as text.
// An answer with block tags (tables, headings, lists) is sent as a rich message, a simple one as a regular HTML message
internal static partial class AgentRichHtml
{
    private static readonly HashSet<string> InlineTags = new(StringComparer.OrdinalIgnoreCase) { "b", "i", "u", "s", "code" };

    // Tags that are available only in rich messages
    private static readonly HashSet<string> BlockTags = new(StringComparer.OrdinalIgnoreCase)
    {
        "p", "br", "hr", "h1", "h2", "h3", "h4", "h5", "h6", "ul", "ol", "li",
        "table", "tr", "th", "td", "caption", "blockquote", "details", "summary", "pre"
    };

    // Elements that hold their own content, so loose text inside them is not wrapped in paragraphs
    private static readonly HashSet<string> ContainerTags = new(StringComparer.OrdinalIgnoreCase)
    {
        "p", "h1", "h2", "h3", "h4", "h5", "h6", "ul", "ol", "table", "blockquote", "details", "pre"
    };

    private static readonly Dictionary<string, string[]> BooleanAttributes = new(StringComparer.OrdinalIgnoreCase)
    {
        ["table"] = ["bordered", "striped", "compact"],
        ["details"] = ["open"],
        ["blockquote"] = ["expandable"]
    };

    internal sealed record class Result(string Html, bool IsRich);

    // Keeps the allowed tags, encodes all other text and makes the result valid for the chosen message type
    internal static Result Prepare(string text)
    {
        var tokens = Tokenize(text);
        var isRich = tokens.Exists(static token => token.IsTag && BlockTags.Contains(token.Name));

        if (isRich is false)
        {
            var simple = new StringBuilder(text.Length);
            foreach (var token in tokens)
            {
                _ = simple.Append(token.Html);
            }

            return new(simple.ToString(), IsRich: false);
        }

        // Line breaks have no meaning in rich HTML, so loose text between blocks becomes paragraphs
        var builder = new StringBuilder(text.Length + 64);
        var loose = new StringBuilder();
        var depth = 0;

        foreach (var token in tokens)
        {
            var isContainer = token.IsTag && ContainerTags.Contains(token.Name);
            if (depth is 0 && isContainer is false && (token.IsTag is false || InlineTags.Contains(token.Name) || token.Name is "br"))
            {
                _ = loose.Append(token.Html);
                continue;
            }

            if (depth is 0)
            {
                FlushLooseText(builder, loose);
            }

            _ = builder.Append(depth > 0 && token.IsTag is false ? token.Html.Replace("\n", " ", StringComparison.Ordinal) : token.Html);

            if (isContainer)
            {
                depth = Math.Max(0, depth + (token.IsClosing ? -1 : 1));
            }
        }

        FlushLooseText(builder, loose);
        return new(builder.ToString(), IsRich: true);
    }

    // Converts a rich answer to the HTML subset of a regular message. It is used when Telegram rejects the rich message
    internal static string ToFallback(string richHtml)
    {
        var text = richHtml;

        text = CellBoundaryRegex().Replace(text, " — ");
        text = HeadingOpenRegex().Replace(text, "\n\n<b>");
        text = HeadingCloseRegex().Replace(text, "</b>\n");
        text = ListItemOpenRegex().Replace(text, "\n• ");
        text = LineBreakRegex().Replace(text, "\n");
        text = ParagraphBoundaryRegex().Replace(text, "\n\n");
        text = RichOnlyTagRegex().Replace(text, string.Empty);
        text = SpaceBeforeLineBreakRegex().Replace(text, "\n");
        text = ExtraLineBreakRegex().Replace(text, "\n\n");

        return text.Trim();
    }

    private static void FlushLooseText(StringBuilder builder, StringBuilder loose)
    {
        var text = loose.ToString().Trim('\n', ' ', '\r');
        _ = loose.Clear();

        if (text.Length is 0)
        {
            return;
        }

        foreach (var paragraph in ParagraphSeparatorRegex().Split(text))
        {
            var content = paragraph.Trim('\n', ' ', '\r');
            if (content.Length > 0)
            {
                _ = builder.Append("<p>").Append(content.Replace("\n", "<br>", StringComparison.Ordinal)).Append("</p>");
            }
        }
    }

    private static List<Token> Tokenize(string text)
    {
        var tokens = new List<Token>();
        var currentIndex = 0;

        foreach (Match match in TagRegex().Matches(text))
        {
            var tag = CreateTag(match);
            if (tag is null)
            {
                continue;
            }

            AddText(tokens, text[currentIndex..match.Index]);
            tokens.Add(tag);
            currentIndex = match.Index + match.Length;
        }

        AddText(tokens, text[currentIndex..]);
        return tokens;
    }

    private static void AddText(List<Token> tokens, string text)
    {
        if (text.Length > 0)
        {
            // The text is decoded first, so entities written by the model are not encoded twice
            tokens.Add(new(WebUtility.HtmlEncode(WebUtility.HtmlDecode(text)), string.Empty, IsTag: false, IsClosing: false));
        }
    }

    // Returns null if the tag or one of its attributes is not allowed. Such a tag is shown to the user as text
    private static Token? CreateTag(Match match)
    {
        var name = match.Groups["name"].Value.ToLowerInvariant();
        var isClosing = match.Groups["close"].Length > 0;
        var attributes = match.Groups["attributes"].Value;

        if (InlineTags.Contains(name) is false && BlockTags.Contains(name) is false)
        {
            return null;
        }

        if (isClosing || string.IsNullOrWhiteSpace(attributes))
        {
            return string.IsNullOrWhiteSpace(attributes) ? new($"<{(isClosing ? "/" : string.Empty)}{name}>", name, IsTag: true, isClosing) : null;
        }

        var builder = new StringBuilder("<").Append(name);
        foreach (Match attribute in AttributeRegex().Matches(attributes))
        {
            var attributeName = attribute.Groups["name"].Value.ToLowerInvariant();
            var hasValue = attribute.Groups["value"].Success;
            var value = attribute.Groups["value"].Value;

            var isAllowed = hasValue
                ? IsAllowedValueAttribute(name, attributeName, value)
                : BooleanAttributes.TryGetValue(name, out var allowed) && Array.IndexOf(allowed, attributeName) >= 0;

            if (isAllowed is false)
            {
                return null;
            }

            _ = builder.Append(' ').Append(attributeName);
            if (hasValue)
            {
                _ = builder.Append("=\"").Append(value.ToLowerInvariant()).Append('"');
            }
        }

        return new(builder.Append('>').ToString(), name, IsTag: true, IsClosing: false);
    }

    private static bool IsAllowedValueAttribute(string tag, string attribute, string value)
    {
        if (tag is not ("th" or "td"))
        {
            return false;
        }

        return attribute switch
        {
            "align" => value.ToLowerInvariant() is "left" or "center" or "right",
            "colspan" or "rowspan" => value.Length is > 0 and <= 2 && int.TryParse(value, out var span) && span > 0,
            _ => false
        };
    }

    private sealed record class Token(string Html, string Name, bool IsTag, bool IsClosing);

    [GeneratedRegex("""<(?<close>/?)(?<name>[A-Za-z][A-Za-z0-9]*)(?<attributes>(?:\s+[A-Za-z-]+(?:="[^"<>]*")?)*)\s*/?>""", RegexOptions.CultureInvariant)]
    private static partial Regex TagRegex();

    [GeneratedRegex("""(?<name>[A-Za-z-]+)(?:="(?<value>[^"<>]*)")?""", RegexOptions.CultureInvariant)]
    private static partial Regex AttributeRegex();

    [GeneratedRegex(@"\n\s*\n", RegexOptions.CultureInvariant)]
    private static partial Regex ParagraphSeparatorRegex();

    [GeneratedRegex(@"</t[hd]>\s*<t[hd][^>]*>", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex CellBoundaryRegex();

    [GeneratedRegex(@"<(?:h[1-6]|summary|caption)>", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex HeadingOpenRegex();

    [GeneratedRegex(@"</(?:h[1-6]|summary|caption)>", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex HeadingCloseRegex();

    [GeneratedRegex(@"<li>", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex ListItemOpenRegex();

    [GeneratedRegex(@"<br>|</tr>", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex LineBreakRegex();

    [GeneratedRegex(@"</?(?:p|table|ul|ol|blockquote|details|pre)(?:\s[^>]*)?>|<hr>", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex ParagraphBoundaryRegex();

    [GeneratedRegex(@"</?(?:tr|th|td|li|u|s)(?:\s[^>]*)?>", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex RichOnlyTagRegex();

    [GeneratedRegex(@"[ \t]+\n|\n[ \t]+", RegexOptions.CultureInvariant)]
    private static partial Regex SpaceBeforeLineBreakRegex();

    [GeneratedRegex(@"\n{3,}", RegexOptions.CultureInvariant)]
    private static partial Regex ExtraLineBreakRegex();
}
