using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Extensions.Localization;
using Xunit;

namespace GarageGroup.Internal.Timesheet.Test;

public static class AgentToolActionPreviewTest
{
    private static readonly DictionaryLocalizer Localizer = new(
        new()
        {
            ["DefaultTitle"] = "Подготовлено действие: {0}",
            ["PreviewHint"] = "Проверьте параметры.",
            ["Title_create-timesheet"] = "Создать списание",
            ["Arg_date"] = "Дата",
            ["Arg_id"] = "Проект",
            ["Arg_delete-timesheet_id"] = "Списание",
            ["Value_Project"] = "Проект"
        });

    [Fact]
    public static void Build_KnownOperation_ExpectLocalizedLabelsResolvedNamesAndEncodedValues()
    {
        var action = CreateAction(
            "create-timesheet",
            new("date", "2026-11-27"),
            new("id", "839bb7ea-6e1f-4ba0-a5fb-a63769a20e65", "R&D <test>"),
            new("type", "Project"),
            new("duration", "2"),
            new("description", "<b>#Test</b>"));

        var (rich, fallback) = AgentToolActionPreview.Build(action, Localizer, "ru");

        Assert.Equal(
            "<p><b>Создать списание</b></p><table compact>" +
            "<tr><th align=\"left\">Дата</th><td>27 ноября 2026, пятница</td></tr>" +
            "<tr><th align=\"left\">Проект</th><td>R&amp;D &lt;test&gt;</td></tr>" +
            "<tr><th align=\"left\">type</th><td>Проект</td></tr>" +
            "<tr><th align=\"left\">duration</th><td>2</td></tr>" +
            "<tr><th align=\"left\">description</th><td>&lt;b&gt;#Test&lt;/b&gt;</td></tr>" +
            "</table><p>Проверьте параметры.</p>",
            rich);

        Assert.Equal(
            "<b>Создать списание</b>\n\n<b>Дата:</b> 27 ноября 2026, пятница\n<b>Проект:</b> R&amp;D &lt;test&gt;\n<b>type:</b> Проект\n" +
            "<b>duration:</b> 2\n<b>description:</b> &lt;b&gt;#Test&lt;/b&gt;\n\nПроверьте параметры.",
            fallback);
    }

    [Fact]
    public static void Build_UnknownOperationAndUnresolvedIdentifier_ExpectDefaultTitleAndRawValue()
    {
        var action = CreateAction("archive-<project>", new AgentActionArgument("id", "839bb7ea-6e1f-4ba0-a5fb-a63769a20e65"));

        var (rich, _) = AgentToolActionPreview.Build(action, Localizer, "ru");

        Assert.Contains("<b>Подготовлено действие: archive-&lt;project&gt;</b>", rich);
        Assert.Contains("<td>839bb7ea-6e1f-4ba0-a5fb-a63769a20e65</td>", rich);
    }

    [Fact]
    public static void Build_ArgumentLabelIsDefinedForOperation_ExpectOperationLabelWins()
    {
        var (rich, _) = AgentToolActionPreview.Build(CreateAction("delete-timesheet", new AgentActionArgument("id", "x", "2026-11-24 E2E 2")), Localizer, "en");

        Assert.Contains("<th align=\"left\">Списание</th><td>24 November 2026, Tuesday E2E 2</td>", rich);
    }

    [Fact]
    public static void BuildCallbackData_GuidActionId_ExpectDataFitsTelegramLimit()
    {
        var data = AgentChatReplySender.BuildCallbackData(Guid.NewGuid().ToString("N"), isConfirmed: true);

        Assert.StartsWith("agent-tool:c:", data);
        Assert.True(Encoding.UTF8.GetByteCount(data) <= 64);
    }

    [Fact]
    public static void EntityCache_ToolResults_ExpectNamesAndSummariesByIdentifier()
    {
        var cache = new AgentEntityCache();

        cache.Remember(
            15,
            """
            {"isSuccess":true,
             "projects":[{"id":"839BB7EA-6E1F-4BA0-A5FB-A63769A20E65","name":"E2E Role test","type":"Project"}],
             "timesheets":[{"id":"27a82cb6-ee49-45a1-a623-75f9304ed000","projectName":"X5","duration":7.5,"description":"#Work","date":"2026-11-24"}]}
            """);
        cache.Remember(15, "not json");

        Assert.Equal("E2E Role test", cache.FindDisplay(15, "839bb7ea-6e1f-4ba0-a5fb-a63769a20e65"));
        Assert.Equal("2026-11-24 · X5 · 7.5 · #Work", cache.FindDisplay(15, "27A82CB6-EE49-45A1-A623-75F9304ED000"));
        Assert.Null(cache.FindDisplay(15, "00000000-0000-0000-0000-000000000001"));
        Assert.Null(cache.FindDisplay(15, "#Test"));

        // Names are kept per user
        Assert.Null(cache.FindDisplay(16, "839bb7ea-6e1f-4ba0-a5fb-a63769a20e65"));
    }

    private static AgentPendingAction CreateAction(string functionName, params AgentActionArgument[] arguments)
        =>
        new("id", new(15, 27), "Timesheet", functionName, "{}", arguments, DateTimeOffset.MaxValue);

    private sealed class DictionaryLocalizer(Dictionary<string, string> values) : IStringLocalizer
    {
        public LocalizedString this[string name]
            =>
            values.TryGetValue(name, out var value) ? new(name, value, resourceNotFound: false) : new(name, name, resourceNotFound: true);

        public LocalizedString this[string name, params object[] arguments]
            =>
            new(name, string.Format(this[name].Value, arguments), this[name].ResourceNotFound);

        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
            =>
            values.Select(static value => new LocalizedString(value.Key, value.Value));
    }
}
