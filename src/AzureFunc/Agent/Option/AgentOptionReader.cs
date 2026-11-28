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

        var tools = ReadTools(section.GetSection("Tools"));
        var signIn = ReadSignIn(section.GetSection("SignIn"), configuration);

        if (signIn is null && Array.Exists(tools, static tool => string.IsNullOrEmpty(tool.Audience) is false))
        {
            throw new InvalidOperationException("Agent:SignIn must be configured because some agent tools require a user access token.");
        }

        return new()
        {
            Foundry = ReadFoundry(section.GetSection("Foundry")),
            Message = ReadMessage(section.GetSection("Message")),
            Storage = new(
                ConnectionString: section["Storage:ConnectionString"].OrNullIfWhiteSpace() ?? configuration["AzureWebJobsStorage"].OrEmpty(),
                ConversationTableName: section["Storage:ConversationTableName"].OrNullIfWhiteSpace() ?? DefaultConversationTableName),
            Tools = tools,
            SignIn = signIn,
            Voice = ReadVoice(section.GetSection("Voice")),
            Prompt = ReadPrompt(section.GetSection("Prompt"))
        };
    }

    private static AgentPromptOption ReadPrompt(IConfigurationSection section)
    {
        var defaults = new AgentPromptOption();
        var cacheMinutes = section.GetValue("CacheMinutes", defaults.CacheDuration.TotalMinutes);

        if (cacheMinutes < 0)
        {
            throw new InvalidOperationException("Agent:Prompt:CacheMinutes must not be negative.");
        }

        return new()
        {
            ContainerName = section["ContainerName"].OrNullIfWhiteSpace() ?? defaults.ContainerName,
            BlobName = section["BlobName"].OrNullIfWhiteSpace() ?? defaults.BlobName,
            CacheDuration = TimeSpan.FromMinutes(cacheMinutes)
        };
    }

    private static AgentVoiceTranscriptionOption? ReadVoice(IConfigurationSection section)
    {
        var deploymentName = section["DeploymentName"].OrNullIfWhiteSpace();
        if (deploymentName is null)
        {
            return null;
        }

        if (Uri.TryCreate(section["Endpoint"], UriKind.Absolute, out var endpoint) is false)
        {
            throw new InvalidOperationException("Agent:Voice:Endpoint must be an absolute URI.");
        }

        return new(endpoint, deploymentName, section["ModelId"].OrNullIfWhiteSpace() ?? deploymentName);
    }

    private static AgentSignInOption? ReadSignIn(IConfigurationSection section, IConfiguration configuration)
    {
        var clientId = section["ClientId"].OrNullIfWhiteSpace();
        if (clientId is null)
        {
            return null;
        }

        var tenantId = section["TenantId"].OrNullIfWhiteSpace()
            ?? throw new InvalidOperationException("Agent:SignIn:TenantId must be specified.");

        if (Uri.TryCreate(section["RedirectUri"], UriKind.Absolute, out var redirectUri) is false || redirectUri.Scheme != Uri.UriSchemeHttps)
        {
            throw new InvalidOperationException("Agent:SignIn:RedirectUri must be an absolute HTTPS URI.");
        }

        byte[] encryptionKey;
        try
        {
            encryptionKey = Convert.FromBase64String(section["TokenEncryptionKey"].OrEmpty());
        }
        catch (FormatException exception)
        {
            throw new InvalidOperationException("Agent:SignIn:TokenEncryptionKey must be a Base64 string.", exception);
        }

        if (encryptionKey.Length is not AesGcmAgentTokenProtector.KeySize)
        {
            throw new InvalidOperationException(
                $"Agent:SignIn:TokenEncryptionKey must contain {AesGcmAgentTokenProtector.KeySize} bytes encoded as Base64.");
        }

        return new(tenantId, clientId, redirectUri, encryptionKey)
        {
            // The identity that calls the chat model is used by default
            ManagedIdentityClientId = section["ManagedIdentityClientId"].OrNullIfWhiteSpace()
                ?? configuration["Agent:Foundry:ManagedIdentityClientId"].OrNullIfWhiteSpace()
        };
    }

    private static AgentFoundryOption ReadFoundry(IConfigurationSection section)
    {
        if (Uri.TryCreate(section["ProjectEndpoint"], UriKind.Absolute, out var endpoint) is false)
        {
            throw new InvalidOperationException("Agent:Foundry:ProjectEndpoint must be an absolute URI.");
        }

        var modelId = section["ModelId"].OrEmpty();
        if (string.IsNullOrWhiteSpace(modelId))
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
            MaxHistoryMessageCount: maxHistoryMessageCount)
        {
            EditableActionCard = section.GetValue("EditableActionCard", false)
        };
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
                    SpecificationSubscriptionKey = toolSection["SpecificationSubscriptionKey"].OrNullIfWhiteSpace(),
                    AllowedOperations = toolSection.GetSection("AllowedOperations").Get<string[]>() ?? [],
                    ExcludedOperations = toolSection.GetSection("ExcludedOperations").Get<string[]>() ?? [],
                    NoConfirmationOperations = toolSection.GetSection("NoConfirmationOperations").Get<string[]>() ?? [],
                    UserSettingsOperations = toolSection.GetSection("UserSettingsOperations").Get<string[]>() ?? []
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
