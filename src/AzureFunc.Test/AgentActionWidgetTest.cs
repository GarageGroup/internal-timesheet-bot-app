using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Localization;
using Xunit;

namespace GarageGroup.Internal.Timesheet.Test;

public static class AgentActionWidgetTest
{
    private const string ProjectId = "839bb7ea-6e1f-4ba0-a5fb-a63769a20e65";

    private const string TimesheetId = "27a82cb6-ee49-45a1-a623-75f9304ed000";

    private static readonly DateOnly Today = new(2026, 11, 28);

    private static readonly Localizer Resources = new(
        new()
        {
            ["Title_create-timesheet"] = "Создать списание",
            ["Title_update-timesheet"] = "Изменить списание",
            ["Arg_date"] = "Дата",
            ["Arg_duration"] = "Длительность",
            ["Arg_description"] = "Комментарий",
            ["Value_Project"] = "Проект",
            ["Value_Lead"] = "Лид",
            ["LabelFrom_id"] = "type",
            ["HoursUnit"] = "ч",
            ["ConfirmButton"] = "✅ Подтвердить",
            ["CancelButton"] = "❌ Отменить",
            ["Cancelled"] = "Действие отменено.",
            ["NotFound"] = "Это действие уже обработано или срок подтверждения истёк.",
            ["SignInRequired"] = "Вход устарел. Напишите мне любое сообщение, войдите заново и нажмите кнопку ещё раз.",
            ["WidgetHint"] = "Пока ничего не изменено. При необходимости поправьте дату и длительность и подтвердите действие.",
            ["WidgetSending"] = "Выполняю…",
            ["WidgetConfirmed"] = "✅ Подтверждено.",
            ["WidgetFailed"] = "Не удалось отправить. Попробуйте ещё раз."
        });

    private static AgentPendingAction CreateAction(string functionName, params AgentActionArgument[] arguments)
        =>
        new("0123456789abcdef0123456789abcdef", new(15, 27), [new("Timesheet", functionName, "{}", arguments)], DateTimeOffset.MaxValue);

    private static AgentPendingAction CreateTimesheetAction(string date = "2026-11-27", string duration = "2", string description = "#Test")
        =>
        CreateAction(
            "create-timesheet",
            new("date", date),
            new("id", ProjectId, "R&D <test>"),
            new("type", "Project"),
            new("duration", duration),
            new("description", description));

    private static JsonElement ReadConfig(string html)
    {
        var start = html.IndexOf("var C=", StringComparison.Ordinal) + "var C=".Length;
        var end = html.IndexOf(";var W=", start, StringComparison.Ordinal);

        using var document = JsonDocument.Parse(html[start..end]);
        return document.RootElement.Clone();
    }

    [Fact]
    public static void TryBuild_CreateTimesheet_ExpectCardWithEditableDateAndDurationAndStaticRows()
    {
        var widget = AgentActionWidget.TryBuild(CreateTimesheetAction(), Resources, "ru", Today);

        Assert.NotNull(widget);
        Assert.Equal("Создать списание", widget.Caption);

        Assert.Contains("<p><b>Создать списание</b></p><table>", widget.Html);
        Assert.Contains("<tr><th>Дата</th><td><s id=do></s><div class=c><button id=dp>", widget.Html);
        Assert.Contains("<tr><th>&#128193; Проект</th><td>R&amp;D &lt;test&gt;</td></tr>", widget.Html);
        Assert.Contains("<tr><th>Длительность</th><td><s id=ho></s><div class=c><button id=hm>", widget.Html);
        Assert.Contains("<button data-q=4>1&nbsp;ч</button><button data-q=8>2&nbsp;ч</button>", widget.Html);
        Assert.Contains("<tr><th>Комментарий</th><td>#Test</td></tr>", widget.Html);
        Assert.Contains("<button id=ok>✅ Подтвердить</button><button id=no>❌ Отменить</button>", widget.Html);

        var config = ReadConfig(widget.Html);

        Assert.Equal("0123456789abcdef0123456789abcdef", config.GetProperty("a").GetString());
        Assert.Equal("2026-11-27", config.GetProperty("d").GetString());
        Assert.Equal(8, config.GetProperty("h").GetInt32());

        // The date may be moved from the first day of the previous month to today
        Assert.Equal("2026-10-01", config.GetProperty("n").GetString());
        Assert.Equal("2026-11-28", config.GetProperty("x").GetString());

        // A new time entry has no old values
        Assert.Equal(JsonValueKind.Null, config.GetProperty("od").ValueKind);
        Assert.Equal(JsonValueKind.Null, config.GetProperty("oh").ValueKind);

        Assert.Equal("ноября", config.GetProperty("M")[10].GetString());
        Assert.Equal("пятница", config.GetProperty("D")[5].GetString());
    }

    [Fact]
    public static void TryBuild_EnglishLocale_ExpectEnglishNamesOfMonthsAndDays()
    {
        var widget = AgentActionWidget.TryBuild(CreateTimesheetAction(), Resources, "en", Today);
        var config = ReadConfig(widget!.Html);

        Assert.Equal("November", config.GetProperty("M")[10].GetString());
        Assert.Equal("Friday", config.GetProperty("D")[5].GetString());
    }

    [Fact]
    public static void TryBuild_UpdateTimesheet_ExpectNewValuesAreChosenAndValuesOfTheEntryAreOld()
    {
        AgentActionArgument[] details =
        [
            new("date", "2026-11-27"), new("id", ProjectId, "Test"), new("type", "Lead"), new("duration", "1.5"), new("description", "Созвон")
        ];

        var action = CreateAction(
            "update-timesheet",
            new AgentActionArgument("timesheetId", TimesheetId, "2026-11-27 Test 1.5") { Details = details },
            new AgentActionArgument("duration", "2"),
            new AgentActionArgument("description", "Созвон с клиентом"));

        var widget = AgentActionWidget.TryBuild(action, Resources, "ru", Today);

        Assert.NotNull(widget);
        Assert.Contains("<p><b>Изменить списание</b></p>", widget.Html);
        Assert.Contains("<tr><th>&#127919; Лид</th><td>Test</td></tr>", widget.Html);
        Assert.Contains("<tr><th>Комментарий</th><td><s>Созвон</s><br>Созвон с клиентом</td></tr>", widget.Html);
        Assert.DoesNotContain("timesheetId", widget.Html);

        var config = ReadConfig(widget.Html);

        // The date is not changed by the update, so the date of the entry is shown
        Assert.Equal("2026-11-27", config.GetProperty("d").GetString());
        Assert.Equal("2026-11-27", config.GetProperty("od").GetString());
        Assert.Equal(8, config.GetProperty("h").GetInt32());
        Assert.Equal(6, config.GetProperty("oh").GetInt32());
    }

    [Fact]
    public static void TryBuild_UserTextWithMarkupAndScript_ExpectTextIsEncodedEverywhere()
    {
        var action = CreateAction(
            "create-timesheet",
            new("date", "2026-11-27"),
            new("id", ProjectId, "</script><script>alert(1)</script>"),
            new("type", "Project"),
            new("duration", "2"),
            new("description", "<b>'x'</b> & </script>"));

        var widget = AgentActionWidget.TryBuild(action, Resources, "ru", Today);

        Assert.NotNull(widget);
        Assert.Equal(1, widget.Html.Split("<script>").Length - 1);
        Assert.Equal(1, widget.Html.Split("</script>").Length - 1);
        Assert.DoesNotContain("<b>'x'</b>", widget.Html);
        Assert.Contains("&lt;b&gt;&#39;x&#39;&lt;/b&gt; &amp; &lt;/script&gt;", widget.Html);
    }

    [Theory]
    [InlineData("2026-11-29", "2")]
    [InlineData("2026-09-30", "2")]
    [InlineData("вчера", "2")]
    [InlineData("2026-11-27", "0")]
    [InlineData("2026-11-27", "2.1")]
    [InlineData("2026-11-27", "25")]
    [InlineData("2026-11-27", "два")]
    public static void TryBuild_DateOrDurationCannotBeEditedInTheCard_ExpectNull(string date, string duration)
        =>
        Assert.Null(AgentActionWidget.TryBuild(CreateTimesheetAction(date, duration), Resources, "ru", Today));

    [Fact]
    public static void TryBuild_ActionIsNotOneTimesheetCall_ExpectNull()
    {
        var delete = CreateAction("delete-timesheet", new AgentActionArgument("timesheetId", TimesheetId));
        var two = CreateTimesheetAction() with
        {
            Calls = [.. CreateTimesheetAction().Calls, .. CreateTimesheetAction().Calls]
        };

        var withoutDuration = CreateAction("create-timesheet", new AgentActionArgument("date", "2026-11-27"));

        Assert.Null(AgentActionWidget.TryBuild(delete, Resources, "ru", Today));
        Assert.Null(AgentActionWidget.TryBuild(two, Resources, "ru", Today));
        Assert.Null(AgentActionWidget.TryBuild(withoutDuration, Resources, "ru", Today));
    }

    // Telegram accepts a widget of 32768 bytes that takes at most 4096 bytes when compressed by zlib
    [Fact]
    public static void TryBuild_LongNamesAndComment_ExpectHtmlWithinTelegramLimits()
    {
        var action = CreateAction(
            "create-timesheet",
            new("date", "2026-11-27"),
            new("id", ProjectId, string.Concat(Enumerable.Repeat("Очень длинное название проекта ", 8))),
            new("type", "Project"),
            new("duration", "7.75"),
            new("description", string.Concat(Enumerable.Range(0, 40).Select(static index => $"Комментарий №{index} про задачу #Task{index * 37}. "))));

        var widget = AgentActionWidget.TryBuild(action, Resources, "ru", Today);
        Assert.NotNull(widget);

        var bytes = Encoding.UTF8.GetBytes(widget.Html);
        Assert.InRange(bytes.Length, 1, 32768);

        using var compressed = new MemoryStream();
        using (var zlib = new ZLibStream(compressed, CompressionLevel.SmallestSize, leaveOpen: true))
        {
            zlib.Write(bytes);
        }

        Assert.InRange(compressed.Length, 1, 4096);
        Assert.DoesNotContain('\0', widget.Html);
    }

    [Theory]
    [InlineData("c:0123456789abcdef0123456789abcdef:20261127:8", "0123456789abcdef0123456789abcdef", "2026-11-27", 2)]
    [InlineData("c:abc:20261001:1", "abc", "2026-10-01", 0.25)]
    [InlineData("c:abc:20261128:96", "abc", "2026-11-28", 24)]
    public static void TryParseQuery_Confirmation_ExpectActionIdAndEditedValues(string data, string expectedId, string expectedDate, double expectedDuration)
    {
        Assert.True(AgentActionWidget.TryParseQuery(data, out var actionId, out var isConfirmed, out var edit));

        Assert.Equal(expectedId, actionId);
        Assert.True(isConfirmed);
        Assert.Equal(new AgentActionEdit(DateOnly.ParseExact(expectedDate, "yyyy-MM-dd"), (decimal)expectedDuration), edit);
        Assert.InRange(Encoding.UTF8.GetByteCount(data), 1, 64);
    }

    [Fact]
    public static void TryParseQuery_Cancellation_ExpectActionIdWithoutEdit()
    {
        Assert.True(AgentActionWidget.TryParseQuery("x:0123456789abcdef", out var actionId, out var isConfirmed, out var edit));

        Assert.Equal("0123456789abcdef", actionId);
        Assert.False(isConfirmed);
        Assert.Null(edit);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("refresh")]
    [InlineData("c:abc")]
    [InlineData("c::20261127:8")]
    [InlineData("c:a-b:20261127:8")]
    [InlineData("c:abc:2026-11-27:8")]
    [InlineData("c:abc:20261332:8")]
    [InlineData("c:abc:20261127:0")]
    [InlineData("c:abc:20261127:97")]
    [InlineData("c:abc:20261127:-1")]
    [InlineData("c:abc:20261127:1.5")]
    [InlineData("c:abc:20261127:8:9")]
    [InlineData("x:abc:1")]
    [InlineData("z:abc")]
    public static void TryParseQuery_UnexpectedData_ExpectFalse(string? data)
        =>
        Assert.False(AgentActionWidget.TryParseQuery(data, out _, out _, out _));

    [Fact]
    public static void BuildChatData_ExpectStatusAndConfirmedValuesWithinTelegramLimit()
    {
        Assert.Equal("""{"s":"ok","d":"2026-11-26","h":14}""", AgentActionWidget.BuildChatData("ok", new(new(2026, 11, 26), 3.5m)));
        Assert.Equal("""{"s":"x"}""", AgentActionWidget.BuildChatData("x"));
    }

    [Fact]
    public static void Apply_EditOfTimesheetCall_ExpectArgumentsJsonAndPreviewArgumentsAreChanged()
    {
        var action = CreateTimesheetAction() with
        {
            Calls =
            [
                CreateTimesheetAction().Calls[0] with
                {
                    ArgumentsJson = """{"date":"2026-11-27","id":"839bb7ea","type":"Project","duration":2,"description":"#Test"}"""
                }
            ]
        };

        var actual = AgentActionEditor.Apply(action, new(new(2026, 11, 26), 3.5m)).Calls[0];

        using var arguments = JsonDocument.Parse(actual.ArgumentsJson);
        Assert.Equal("2026-11-26", arguments.RootElement.GetProperty("date").GetString());
        Assert.Equal(3.5m, arguments.RootElement.GetProperty("duration").GetDecimal());
        Assert.Equal("#Test", arguments.RootElement.GetProperty("description").GetString());

        Assert.Equal("2026-11-26", actual.Arguments.Single(static argument => argument.Name is "date").Value);
        Assert.Equal("3.5", actual.Arguments.Single(static argument => argument.Name is "duration").Value);
        Assert.Equal("R&D <test>", actual.Arguments.Single(static argument => argument.Name is "id").Display);
    }

    [Fact]
    public static void Apply_UpdateWithoutDateAndDurationArguments_ExpectArgumentsAreAdded()
    {
        var action = CreateAction("update-timesheet", new AgentActionArgument("timesheetId", TimesheetId)) with { };
        action = action with { Calls = [action.Calls[0] with { ArgumentsJson = $$"""{"timesheetId":"{{TimesheetId}}"}""" }] };

        var actual = AgentActionEditor.Apply(action, new(new(2026, 11, 26), 0.25m)).Calls[0];

        Assert.Equal($$"""{"timesheetId":"{{TimesheetId}}","date":"2026-11-26","duration":0.25}""", actual.ArgumentsJson);
        Assert.Equal(["timesheetId", "date", "duration"], actual.Arguments.Select(static argument => argument.Name));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(0.1)]
    [InlineData(24.25)]
    [InlineData(-1)]
    public static void Apply_InvalidDuration_ExpectDurationOfTheModelIsKept(double duration)
    {
        var action = CreateTimesheetAction() with { };
        action = action with { Calls = [action.Calls[0] with { ArgumentsJson = """{"date":"2026-11-27","duration":2}""" }] };

        var actual = AgentActionEditor.Apply(action, new(null, (decimal)duration)).Calls[0];

        Assert.Equal("""{"date":"2026-11-27","duration":2}""", actual.ArgumentsJson);
    }

    [Fact]
    public static void Apply_NoEditOrNotEditableAction_ExpectSameAction()
    {
        var create = CreateTimesheetAction();
        var delete = CreateAction("delete-timesheet", new AgentActionArgument("timesheetId", TimesheetId));

        Assert.Same(create, AgentActionEditor.Apply(create, null));
        Assert.Same(delete, AgentActionEditor.Apply(delete, new(new(2026, 11, 26), 3)));
    }

    private sealed class Localizer(Dictionary<string, string> values) : IStringLocalizer
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
