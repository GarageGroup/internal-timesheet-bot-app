using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace GarageGroup.Internal.Timesheet;

// Remembers objects returned by the tools (projects, time entries) so that identifiers in a prepared action
// can be shown to the user as names. The names come from the API, not from the model, so the preview can be trusted
internal sealed partial class AgentEntityCache
{
    private const int MaxEntitiesPerUser = 500;

    private const int MaxDepth = 6;

    // Fields that describe an object without a name, in the order they are shown
    private static readonly string[] SummaryFields = ["date", "projectName", "duration", "description"];

    // A value that the preview replaces with a localized text, for example the project type
    internal static string CreateValueToken(string value)
        =>
        $"\u27E6{value}\u27E7";

    [GeneratedRegex("\u27E6([A-Za-z0-9_]+)\u27E7", RegexOptions.CultureInvariant)]
    internal static partial Regex ValueTokenRegex();

    private readonly ConcurrentDictionary<long, UserEntities> users = new();

    public void Remember(long telegramUserId, string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return;
        }

        try
        {
            using var document = JsonDocument.Parse(json);
            Remember(users.GetOrAdd(telegramUserId, static _ => new()), document.RootElement, depth: 0);
        }
        catch (JsonException)
        {
            // The tool result is not JSON: there is nothing to remember
        }
    }

    public string? FindDisplay(long telegramUserId, string id)
        =>
        users.TryGetValue(telegramUserId, out var entities) ? entities.Find(id) : null;

    private static void Remember(UserEntities entities, JsonElement element, int depth)
    {
        if (depth > MaxDepth)
        {
            return;
        }

        if (element.ValueKind is JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
            {
                Remember(entities, item, depth + 1);
            }

            return;
        }

        if (element.ValueKind is not JsonValueKind.Object)
        {
            return;
        }

        if (element.TryGetProperty("id", out var idElement) && idElement.ValueKind is JsonValueKind.String &&
            Guid.TryParse(idElement.GetString(), out var id) && BuildDisplay(element) is string display)
        {
            entities.Set(id.ToString("D"), display);
        }

        foreach (var property in element.EnumerateObject())
        {
            Remember(entities, property.Value, depth + 1);
        }
    }

    private static string? BuildDisplay(JsonElement element)
    {
        if (element.TryGetProperty("name", out var name) && name.ValueKind is JsonValueKind.String &&
            string.IsNullOrWhiteSpace(name.GetString()) is false)
        {
            return name.GetString();
        }

        var parts = SummaryFields.Select(field => GetSummaryText(element, field)).Where(static text => string.IsNullOrWhiteSpace(text) is false).ToArray();
        return parts.Length > 0 ? string.Join(" · ", parts) : null;
    }

    // The project is shown as "<type> <name>", the same way as everywhere else
    private static string? GetSummaryText(JsonElement element, string field)
    {
        var text = GetText(element, field);
        if (field is not "projectName" || string.IsNullOrWhiteSpace(text))
        {
            return text;
        }

        var projectType = GetText(element, "projectType");
        return string.IsNullOrWhiteSpace(projectType) || ValueTokenRegex().IsMatch(CreateValueToken(projectType)) is false
            ? text
            : $"{CreateValueToken(projectType)} {text}";
    }

    private static string? GetText(JsonElement element, string field)
    {
        if (element.TryGetProperty(field, out var value) is false)
        {
            return null;
        }

        return value.ValueKind switch
        {
            JsonValueKind.String => value.GetString(),
            JsonValueKind.Number => value.GetDecimal().ToString("0.##", CultureInfo.InvariantCulture),
            _ => null
        };
    }

    private sealed class UserEntities
    {
        private readonly Dictionary<string, string> values = new(StringComparer.OrdinalIgnoreCase);

        private readonly Queue<string> order = new();

        internal void Set(string id, string display)
        {
            lock (values)
            {
                if (values.ContainsKey(id) is false)
                {
                    order.Enqueue(id);
                    if (order.Count > MaxEntitiesPerUser)
                    {
                        _ = values.Remove(order.Dequeue());
                    }
                }

                values[id] = display;
            }
        }

        internal string? Find(string id)
        {
            lock (values)
            {
                return Guid.TryParse(id, out var guid) && values.TryGetValue(guid.ToString("D"), out var display) ? display : null;
            }
        }
    }
}
