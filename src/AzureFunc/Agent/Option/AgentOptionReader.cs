using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;

namespace GarageGroup.Internal.Timesheet;

internal static partial class AgentOptionReader
{
    private const string DefaultTokenScope = "https://ai.azure.com/.default";

    private const string DefaultConversationTableName = "TimesheetBotAgentConversation";

    internal static AgentOption Read(IConfiguration configuration)
    {
        var section = configuration.GetSection("Agent");
        var enabled = section.GetValue("Enabled", false);

        return new()
        {
            Enabled = enabled,
            Foundry = ReadFoundry(section.GetSection("Foundry"), enabled),
            Message = ReadMessage(section.GetSection("Message")),
            Storage = new(
                ConnectionString: section["Storage:ConnectionString"].OrNullIfWhiteSpace() ?? configuration["AzureWebJobsStorage"].OrEmpty(),
                ConversationTableName: section["Storage:ConversationTableName"].OrNullIfWhiteSpace() ?? DefaultConversationTableName),
            Tools = ReadTools(section.GetSection("Tools"))
        };
    }

    private static AgentFoundryOption ReadFoundry(IConfigurationSection section, bool enabled)
    {
        var endpointText = section["ProjectEndpoint"];
        if (Uri.TryCreate(endpointText, UriKind.Absolute, out var endpoint) is false)
        {
            if (enabled)
            {
                throw new InvalidOperationException("Agent:Foundry:ProjectEndpoint must be an absolute URI.");
            }

            endpoint = new("https://localhost/");
        }

        var modelId = section["ModelId"].OrEmpty();
        if (enabled && string.IsNullOrWhiteSpace(modelId))
        {
            throw new InvalidOperationException("Agent:Foundry:ModelId must be specified.");
        }

        return new(endpoint, modelId, section["TokenScope"].OrNullIfWhiteSpace() ?? DefaultTokenScope)
        {
            ManagedIdentityClientId = section["ManagedIdentityClientId"].OrNullIfWhiteSpace()
        };
    }

    private static AgentMessageOption ReadMessage(IConfigurationSection section)
    {
        var maxTextLength = section.GetValue("MaxTextLength", 2000);
        var maxHistoryMessageCount = section.GetValue("MaxHistoryMessageCount", 20);

        if (maxTextLength <= 0 || maxHistoryMessageCount < 0)
        {
            throw new InvalidOperationException("Agent:Message limits must not be negative.");
        }

        return new(
            TimeZone: TimeZoneInfo.FindSystemTimeZoneById(section["TimeZoneId"].OrNullIfWhiteSpace() ?? "Europe/Moscow"),
            MaxTextLength: maxTextLength,
            MaxHistoryMessageCount: maxHistoryMessageCount);
    }

    private static AgentToolOption[] ReadTools(IConfigurationSection section)
    {
        var tools = new List<AgentToolOption>();
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var toolSection in section.GetChildren())
        {
            var name = toolSection["Name"].OrEmpty();
            if (ToolNameRegex().IsMatch(name) is false)
            {
                throw new InvalidOperationException(
                    $"Agent tool name '{name}' is invalid. Only latin letters, digits and underscores are allowed.");
            }

            if (names.Add(name) is false)
            {
                throw new InvalidOperationException($"Agent tool name '{name}' is duplicated.");
            }

            if (Enum.TryParse<AgentToolType>(toolSection["Type"], ignoreCase: true, out var type) is false)
            {
                throw new InvalidOperationException($"Agent tool '{name}' type must be OpenApi or Mcp.");
            }

            if (Uri.TryCreate(toolSection["Url"], UriKind.Absolute, out var url) is false)
            {
                throw new InvalidOperationException($"Agent tool '{name}' Url must be an absolute URI.");
            }

            Uri? baseAddress = null;
            var baseAddressText = toolSection["BaseAddress"];

            if (string.IsNullOrWhiteSpace(baseAddressText) is false &&
                Uri.TryCreate(baseAddressText, UriKind.Absolute, out baseAddress) is false)
            {
                throw new InvalidOperationException($"Agent tool '{name}' BaseAddress must be an absolute URI.");
            }

            if (type is AgentToolType.OpenApi && baseAddress is null)
            {
                // Without an explicit base address the server URL would be taken from the specification,
                // so the user access token could be sent to a host the bot owner did not choose
                throw new InvalidOperationException($"Agent tool '{name}' BaseAddress must be specified for an OpenApi tool.");
            }

            tools.Add(
                new(name, type, url)
                {
                    BaseAddress = baseAddress,
                    Audience = toolSection["Audience"].OrNullIfWhiteSpace(),
                    SubscriptionKey = toolSection["SubscriptionKey"].OrNullIfWhiteSpace(),
                    AllowedOperations = toolSection.GetSection("AllowedOperations").Get<string[]>() ?? []
                });
        }

        return [.. tools];
    }

    private static string? OrNullIfWhiteSpace(this string? value)
        =>
        string.IsNullOrWhiteSpace(value) ? null : value;

    [GeneratedRegex("^[A-Za-z][A-Za-z0-9_]{0,31}$", RegexOptions.CultureInvariant)]
    private static partial Regex ToolNameRegex();
}
