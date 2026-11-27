using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace GarageGroup.Internal.Timesheet.Test;

public static class AgentOptionReaderTest
{
    [Fact]
    public static void Read_OnlyFoundryIsConfigured_ExpectDefaults()
    {
        var actual = AgentOptionReader.Read(BuildConfiguration([]));

        Assert.Empty(actual.Tools);
        Assert.Null(actual.SignIn);
        Assert.Null(actual.Voice);
        Assert.Equal(2000, actual.Message.MaxTextLength);
        Assert.Equal(20, actual.Message.MaxHistoryMessageCount);
        Assert.Equal("TimesheetBotAgentConversation", actual.Storage.ConversationTableName);
        Assert.Equal("https://ai.azure.com/.default", actual.Foundry.TokenScope);
    }

    [Theory]
    [InlineData("Agent:Foundry:ProjectEndpoint")]
    [InlineData("Agent:Foundry:ModelId")]
    public static void Read_FoundryIsNotConfigured_ExpectInvalidOperationException(string key)
        =>
        Assert.Throws<InvalidOperationException>(
            () => AgentOptionReader.Read(BuildConfiguration(new() { [key] = string.Empty })));

    [Fact]
    public static void Read_VoiceIsConfigured_ExpectParsedVoiceWithDeploymentAsDefaultModel()
    {
        var actual = AgentOptionReader.Read(
            BuildConfiguration(
                new()
                {
                    ["Agent:Voice:Endpoint"] = "https://ai.example.openai.azure.com/",
                    ["Agent:Voice:DeploymentName"] = "whisper"
                }));

        Assert.Equal(new("https://ai.example.openai.azure.com/"), actual.Voice?.Endpoint);
        Assert.Equal("whisper", actual.Voice?.DeploymentName);
        Assert.Equal("whisper", actual.Voice?.ModelId);
    }

    [Fact]
    public static void Read_ToolsAreConfigured_ExpectParsedTools()
    {
        var actual = AgentOptionReader.Read(
            BuildConfiguration(
                new()
                {
                    ["AzureWebJobsStorage"] = "UseDevelopmentStorage=true",
                    ["Agent:SignIn:TenantId"] = "tenant",
                    ["Agent:SignIn:ClientId"] = "client",
                    ["Agent:SignIn:RedirectUri"] = "https://apim.example/oauth2/callback",
                    ["Agent:SignIn:TokenEncryptionKey"] = Convert.ToBase64String(new byte[32]),
                    ["Agent:Tools:0:Name"] = "Timesheet",
                    ["Agent:Tools:0:Type"] = "OpenApi",
                    ["Agent:Tools:0:Url"] = "https://apim.example/swagger/swagger.json",
                    ["Agent:Tools:0:BaseAddress"] = "https://apim.example/timesheet/",
                    ["Agent:Tools:0:Audience"] = "api://timesheet",
                    ["Agent:Tools:0:SubscriptionKey"] = "key",
                    ["Agent:Tools:0:SpecificationSubscriptionKey"] = "specification-key",
                    ["Agent:Tools:0:AllowedOperations:0"] = "GetTimesheets",
                    ["Agent:Tools:0:ConfirmationOperations:0"] = "CreateTimesheet",
                    ["Agent:Tools:1:Name"] = "Docs",
                    ["Agent:Tools:1:Type"] = "mcp",
                    ["Agent:Tools:1:Url"] = "https://apim.example/docs/mcp"
                }));

        Assert.Equal("UseDevelopmentStorage=true", actual.Storage.ConnectionString);
        Assert.Equal("https://ai.azure.com/.default", actual.Foundry.TokenScope);

        Assert.Collection(
            actual.Tools,
            tool =>
            {
                Assert.Equal("Timesheet", tool.Name);
                Assert.Equal(AgentToolType.OpenApi, tool.Type);
                Assert.Equal(new Uri("https://apim.example/timesheet/"), tool.BaseAddress);
                Assert.Equal("api://timesheet", tool.Audience);
                Assert.Equal("key", tool.SubscriptionKey);
                Assert.Equal("specification-key", tool.SpecificationSubscriptionKey);
                Assert.Equal(["GetTimesheets"], tool.AllowedOperations);
                Assert.Equal(["CreateTimesheet"], tool.ConfirmationOperations);
            },
            tool =>
            {
                Assert.Equal("Docs", tool.Name);
                Assert.Equal(AgentToolType.Mcp, tool.Type);
                Assert.Null(tool.Audience);
                Assert.Empty(tool.AllowedOperations);
            });
    }

    [Theory]
    [InlineData("Agent:Tools:0:BaseAddress", "")]
    [InlineData("Agent:Tools:0:Name", "time sheet")]
    [InlineData("Agent:Tools:0:Type", "Soap")]
    [InlineData("Agent:Tools:0:Url", "swagger.json")]
    public static void Read_ToolIsInvalid_ExpectInvalidOperationException(string key, string value)
    {
        var settings = new Dictionary<string, string?>
        {
            ["Agent:Tools:0:Name"] = "Timesheet",
            ["Agent:Tools:0:Type"] = "OpenApi",
            ["Agent:Tools:0:Url"] = "https://apim.example/swagger/swagger.json",
            ["Agent:Tools:0:BaseAddress"] = "https://apim.example/timesheet/",
            [key] = value
        };

        _ = Assert.Throws<InvalidOperationException>(() => AgentOptionReader.Read(BuildConfiguration(settings)));
    }

    [Fact]
    public static void Read_ToolNamesAreDuplicated_ExpectInvalidOperationException()
    {
        var settings = new Dictionary<string, string?>
        {
            ["Agent:Tools:0:Name"] = "Docs",
            ["Agent:Tools:0:Type"] = "Mcp",
            ["Agent:Tools:0:Url"] = "https://apim.example/docs/mcp",
            ["Agent:Tools:1:Name"] = "docs",
            ["Agent:Tools:1:Type"] = "Mcp",
            ["Agent:Tools:1:Url"] = "https://apim.example/other/mcp"
        };

        _ = Assert.Throws<InvalidOperationException>(() => AgentOptionReader.Read(BuildConfiguration(settings)));
    }

    [Fact]
    public static void Read_SignInIsConfigured_ExpectParsedSignInWithAgentIdentityByDefault()
    {
        var actual = AgentOptionReader.Read(
            BuildConfiguration(
                new()
                {
                    ["Agent:Foundry:ManagedIdentityClientId"] = "agent-identity",
                    ["Agent:SignIn:TenantId"] = "tenant",
                    ["Agent:SignIn:ClientId"] = "client",
                    ["Agent:SignIn:RedirectUri"] = "https://apim.example/oauth2/callback",
                    ["Agent:SignIn:TokenEncryptionKey"] = Convert.ToBase64String(new byte[32])
                }));

        Assert.NotNull(actual.SignIn);
        Assert.Equal("tenant", actual.SignIn.TenantId);
        Assert.Equal("client", actual.SignIn.ClientId);
        Assert.Equal(new Uri("https://apim.example/oauth2/callback"), actual.SignIn.RedirectUri);
        Assert.Equal(32, actual.SignIn.TokenEncryptionKey.Length);
        Assert.Equal("agent-identity", actual.SignIn.ManagedIdentityClientId);
    }

    [Theory]
    [InlineData("Agent:SignIn:TenantId", "")]
    [InlineData("Agent:SignIn:RedirectUri", "http://apim.example/oauth2/callback")]
    [InlineData("Agent:SignIn:TokenEncryptionKey", "not base64")]
    [InlineData("Agent:SignIn:TokenEncryptionKey", "AAAA")]
    public static void Read_SignInIsInvalid_ExpectInvalidOperationException(string key, string value)
    {
        var settings = new Dictionary<string, string?>
        {
            ["Agent:SignIn:TenantId"] = "tenant",
            ["Agent:SignIn:ClientId"] = "client",
            ["Agent:SignIn:RedirectUri"] = "https://apim.example/oauth2/callback",
            ["Agent:SignIn:TokenEncryptionKey"] = Convert.ToBase64String(new byte[32]),
            [key] = value
        };

        _ = Assert.Throws<InvalidOperationException>(() => AgentOptionReader.Read(BuildConfiguration(settings)));
    }

    [Fact]
    public static void Read_ToolRequiresUserTokenAndSignInIsNotConfigured_ExpectInvalidOperationException()
    {
        var settings = new Dictionary<string, string?>
        {
            ["Agent:Tools:0:Name"] = "Timesheet",
            ["Agent:Tools:0:Type"] = "OpenApi",
            ["Agent:Tools:0:Url"] = "https://apim.example/swagger/swagger.json",
            ["Agent:Tools:0:BaseAddress"] = "https://apim.example/timesheet/",
            ["Agent:Tools:0:Audience"] = "api://timesheet"
        };

        _ = Assert.Throws<InvalidOperationException>(() => AgentOptionReader.Read(BuildConfiguration(settings)));
    }

    // The chat model settings are always required, so every configuration starts from them
    private static IConfiguration BuildConfiguration(Dictionary<string, string?> settings)
    {
        var values = new Dictionary<string, string?>
        {
            ["Agent:Foundry:ProjectEndpoint"] = "https://foundry.example/api/projects/test",
            ["Agent:Foundry:ModelId"] = "gpt-test"
        };

        foreach (var setting in settings)
        {
            values[setting.Key] = setting.Value;
        }

        return new ConfigurationBuilder().AddInMemoryCollection(values).Build();
    }
}
