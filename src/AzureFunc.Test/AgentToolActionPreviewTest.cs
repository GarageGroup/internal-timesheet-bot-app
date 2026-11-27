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
            ["PreviewHintMany"] = "Проверьте действия и подтвердите их все.",
            ["PreviewTitleMany"] = "Подготовлено действий: {0}",
            ["Title_create-timesheet"] = "Создать списание",
            ["Arg_date"] = "Дата",
            ["Arg_id"] = "Проект",
            ["Arg_delete-timesheet_id"] = "Списание",
            ["Value_Project"] = "Проект",
            ["Value_Lead"] = "Лид",
            ["LabelFrom_create-timesheet_id"] = "type",
            ["LabelFrom_id"] = "type"
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
            "<tr><th align=\"left\">duration</th><td>2</td></tr>" +
            "<tr><th align=\"left\">description</th><td>&lt;b&gt;#Test&lt;/b&gt;</td></tr>" +
            "</table><p>Проверьте параметры.</p>",
            rich);

        Assert.Equal(
            "<b>Создать списание</b>\n\n<b>Дата:</b> 27 ноября 2026, пятница\n<b>Проект:</b> R&amp;D &lt;test&gt;\n" +
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
    public static void Build_ProjectOfAnotherType_ExpectOneRowWithTypeAsLabelAndNameAsValue()
    {
        var action = CreateAction(
            "create-timesheet",
            new AgentActionArgument("id", "839bb7ea-6e1f-4ba0-a5fb-a63769a20e65", "Test"),
            new AgentActionArgument("type", "Lead"));

        var (rich, fallback) = AgentToolActionPreview.Build(action, Localizer, "ru");

        Assert.Contains("<table compact><tr><th align=\"left\">Лид</th><td>Test</td></tr></table>", rich);
        Assert.Contains("\n<b>Лид:</b> Test\n", fallback);
    }

    [Fact]
    public static void Build_TypeArgumentWithoutProject_ExpectTypeIsShownAsOwnRow()
    {
        var (rich, _) = AgentToolActionPreview.Build(
            CreateAction("update-timesheet", new AgentActionArgument("type", "Lead")), Localizer, "ru");

        Assert.Contains("<tr><th align=\"left\">type</th><td>Лид</td></tr>", rich);
    }

    [Fact]
    public static void Build_TimeEntrySummary_ExpectLocalizedProjectTypeBeforeProjectName()
    {
        var cache = new AgentEntityCache();
        cache.Remember(
            15,
            """{"timesheets":[{"id":"27a82cb6-ee49-45a1-a623-75f9304ed000","projectType":"Lead","projectName":"Test","duration":2,"date":"2026-11-27"}]}""");

        var display = cache.FindDisplay(15, "27a82cb6-ee49-45a1-a623-75f9304ed000");
        var (rich, _) = AgentToolActionPreview.Build(
            CreateAction("delete-timesheet", new AgentActionArgument("id", "27a82cb6-ee49-45a1-a623-75f9304ed000", display)), Localizer, "ru");

        Assert.Contains("<td>27 ноября 2026, пятница · Лид Test · 2</td>".Replace("·", "&#183;"), rich);
    }

    [Fact]
    public static void Build_DeleteTimeEntry_ExpectFieldsOfTheEntryAsRowsLikeOnCreation()
    {
        var cache = new AgentEntityCache();
        cache.Remember(
            15,
            """
            {"timesheets":[{"id":"27a82cb6-ee49-45a1-a623-75f9304ed000","projectId":"839bb7ea-6e1f-4ba0-a5fb-a63769a20e65",
              "projectType":"Lead","projectName":"Test","duration":1.5,"description":"Созвон","date":"2026-11-27","isActive":true}]}
            """);

        var entity = cache.Find(15, "27a82cb6-ee49-45a1-a623-75f9304ed000");
        var action = CreateAction(
            "delete-timesheet",
            new AgentActionArgument("timesheetId", "27a82cb6-ee49-45a1-a623-75f9304ed000", entity?.Display) { Details = entity?.Details });

        var (rich, fallback) = AgentToolActionPreview.Build(action, Localizer, "ru");

        Assert.Contains(
            "<table compact>" +
            "<tr><th align=\"left\">Дата</th><td>27 ноября 2026, пятница</td></tr>" +
            "<tr><th align=\"left\">Лид</th><td>Test</td></tr>" +
            "<tr><th align=\"left\">duration</th><td>1.5</td></tr>" +
            "<tr><th align=\"left\">description</th><td>Созвон</td></tr>" +
            "</table>",
            rich);

        Assert.Contains("\n<b>Дата:</b> 27 ноября 2026, пятница\n<b>Лид:</b> Test\n", fallback);
        Assert.DoesNotContain("timesheetId", rich);
    }

    [Fact]
    public static void Build_UpdateTimeEntryWithNewValues_ExpectEntryIsOneRowAndNewValuesAreRows()
    {
        var details = new AgentActionArgument[] { new("date", "2026-11-27"), new("duration", "1.5") };
        var action = CreateAction(
            "update-timesheet",
            new AgentActionArgument("timesheetId", "27a82cb6-ee49-45a1-a623-75f9304ed000", "2026-11-27 Test 1.5") { Details = details },
            new AgentActionArgument("duration", "2"));

        var (rich, _) = AgentToolActionPreview.Build(action, Localizer, "ru");

        Assert.Contains("<th align=\"left\">timesheetId</th><td>27 ноября 2026, пятница Test 1.5</td>", rich);
        Assert.Contains("<th align=\"left\">duration</th><td>2</td>", rich);
    }

    [Fact]
    public static void Build_DeleteWithUnknownEntry_ExpectIdentifierRow()
    {
        var (rich, _) = AgentToolActionPreview.Build(
            CreateAction("delete-timesheet", new AgentActionArgument("timesheetId", "27a82cb6-ee49-45a1-a623-75f9304ed000")), Localizer, "ru");

        Assert.Contains("<th align=\"left\">timesheetId</th><td>27a82cb6-ee49-45a1-a623-75f9304ed000</td>", rich);
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

    [Fact]
    public static void Build_SeveralCalls_ExpectCommonHeadingAndNumberedActions()
    {
        var action = new AgentPendingAction(
            "id",
            new(15, 27),
            [
                new("Timesheet", "create-timesheet", "{}", [new("date", "2026-11-23"), new("duration", "2")]),
                new("Timesheet", "create-timesheet", "{}", [new("date", "2026-11-24"), new("duration", "3")])
            ],
            DateTimeOffset.MaxValue);

        var (rich, fallback) = AgentToolActionPreview.Build(action, Localizer, "ru");

        Assert.StartsWith("<h3>Подготовлено действий: 2</h3><p><b>1. Создать списание</b></p><table compact>", rich);
        Assert.Contains("</table><p><b>2. Создать списание</b></p><table compact>", rich);
        Assert.EndsWith("<p>Проверьте действия и подтвердите их все.</p>", rich);

        Assert.StartsWith("<b>Подготовлено действий: 2</b>\n\n<b>1. Создать списание</b>\n", fallback);
        Assert.Contains("\n\n<b>2. Создать списание</b>\n", fallback);
    }

    private static AgentPendingAction CreateAction(string functionName, params AgentActionArgument[] arguments)
        =>
        new("id", new(15, 27), [new("Timesheet", functionName, "{}", arguments)], DateTimeOffset.MaxValue);

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
