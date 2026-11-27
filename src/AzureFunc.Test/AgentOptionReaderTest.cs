using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace GarageGroup.Internal.Timesheet.Test;

public static class AgentOptionReaderTest
{
    [Fact]
    public static void Read_AgentIsNotConfigured_ExpectDisabledOptionWithDefaults()
    {
        var actual = AgentOptionReader.Read(BuildConfiguration([]));

        Assert.False(actual.Enabled);
        Assert.Empty(actual.Tools);
        Assert.Equal(2000, actual.Message.MaxTextLength);
        Assert.Equal(20, actual.Message.MaxHistoryMessageCount);
        Assert.Equal("TimesheetBotAgentConversation", actual.Storage.ConversationTableName);
    }

    [Fact]
    public static void Read_ToolsAreConfigured_ExpectParsedTools()
    {
        var actual = AgentOptionReader.Read(
            BuildConfiguration(
                new()
                {
                    ["AzureWebJobsStorage"] = "UseDevelopmentStorage=true",
                    ["Agent:Enabled"] = "true",
                    ["Agent:Foundry:ProjectEndpoint"] = "https://foundry.example/api/projects/test",
                    ["Agent:Foundry:ModelId"] = "gpt-test",
                    ["Agent:Tools:0:Name"] = "Timesheet",
                    ["Agent:Tools:0:Type"] = "OpenApi",
                    ["Agent:Tools:0:Url"] = "https://apim.example/swagger/swagger.json",
                    ["Agent:Tools:0:BaseAddress"] = "https://apim.example/timesheet/",
                    ["Agent:Tools:0:Audience"] = "api://timesheet",
                    ["Agent:Tools:0:SubscriptionKey"] = "key",
                    ["Agent:Tools:0:AllowedOperations:0"] = "GetTimesheets",
                    ["Agent:Tools:1:Name"] = "Docs",
                    ["Agent:Tools:1:Type"] = "mcp",
                    ["Agent:Tools:1:Url"] = "https://apim.example/docs/mcp"
                }));

        Assert.True(actual.Enabled);
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
                Assert.Equal(["GetTimesheets"], tool.AllowedOperations);
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
    public static void Read_AgentIsEnabledWithoutFoundry_ExpectInvalidOperationException()
        =>
        Assert.Throws<InvalidOperationException>(
            () => AgentOptionReader.Read(BuildConfiguration(new() { ["Agent:Enabled"] = "true" })));

    private static IConfiguration BuildConfiguration(Dictionary<string, string?> settings)
        =>
        new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
}
