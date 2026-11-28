using System;
using System.Collections.Generic;

namespace GarageGroup.Internal.Timesheet;

// A project type is shown as an icon instead of its name: the icon is short and the same in every language.
// The meaning of the icons is described in the system prompt, so the agent explains them when the user asks
internal static class AgentProjectTypeIcon
{
    internal const string Project = "📁";

    internal const string Opportunity = "🤝";

    internal const string Lead = "🎯";

    internal const string Incident = "🛠️";

    // The type comes as the service names it or, from the model, in the language of the answer
    private static readonly Dictionary<string, string> Icons = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Project"] = Project,
        ["Проект"] = Project,
        ["Opportunity"] = Opportunity,
        ["Сделка"] = Opportunity,
        ["Lead"] = Lead,
        ["Лид"] = Lead,
        ["Incident"] = Incident,
        ["Инцидент"] = Incident
    };

    // Returns null for a text that is not a project type: such a text is shown as it is
    internal static string? Find(string? type)
        =>
        string.IsNullOrWhiteSpace(type) is false && Icons.TryGetValue(type.Trim(), out var icon) ? icon : null;
}
