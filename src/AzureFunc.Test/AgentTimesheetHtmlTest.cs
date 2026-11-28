using System;
using System.Linq;
using System.Text;
using Xunit;

namespace GarageGroup.Internal.Timesheet.Test;

public static class AgentTimesheetHtmlTest
{
    private static readonly AgentTimesheetLabels Labels = new(Total: "Итого", PeriodTotal: "Итого за период", HoursUnit: "ч");

    [Fact]
    public static void Expand_TextWithoutBlock_ExpectTextIsKept()
    {
        var actual = AgentTimesheetHtml.Expand("За этот день списаний нет.", Labels, "ru");

        Assert.Equal(["За этот день списаний нет."], actual);
    }

    [Fact]
    public static void Expand_OneDay_ExpectDateTableWithBoldHoursAndTotal()
    {
        const string source =
            "<h3>✅ Списание создано</h3>\n" +
            "<timesheets>\n<day date=\"2026-11-27\">\n" +
            "<entry hours=\"2\" type=\"Проект\"><project>E2E Role test</project><comment>#Test</comment></entry>\n" +
            "<entry hours=\"1.5\" type=\"Сделка\"><project>AMBY. CRM for Real Estate</project></entry>\n" +
            "</day>\n</timesheets>";

        var actual = AgentTimesheetHtml.Expand(source, Labels, "ru");

        Assert.Equal(
            [
                "<h3>✅ Списание создано</h3>\n" +
                "<p>27 ноября 2026, пятница</p>" +
                "<table bordered compact>" +
                "<tr><td><i>Проект</i> <b>E2E Role test</b><br>#Test</td><td align=\"right\"><b>2 ч</b></td></tr>" +
                "<tr><td><i>Сделка</i> <b>AMBY. CRM for Real Estate</b></td><td align=\"right\"><b>1.5 ч</b></td></tr>" +
                "<tr><td><b>Итого</b></td><td align=\"right\"><b>3.5 ч</b></td></tr>" +
                "</table>"
            ],
            actual);
    }

    [Fact]
    public static void Expand_Period_ExpectOneTableGroupedByDaysInCalendarOrder()
    {
        const string source =
            "<h3>Списания за период</h3>" +
            "<timesheets from=\"2026-11-23\" to=\"2026-11-29\">" +
            "<day date=\"2026-11-27\"><entry hours=\"0.25\" type=\"Лид\"><project>Test</project><comment>a</comment></entry></day>" +
            "<day date=\"2026-11-23\">" +
            "<entry hours=\"8\" type=\"Проект\"><project>Internal IT</project><comment>b</comment></entry>" +
            "<entry hours=\"0,5\" type=\"Проект\"><project>Internal IT</project></entry>" +
            "</day>" +
            "<day date=\"2026-11-25\"></day>" +
            "</timesheets>";

        var actual = AgentTimesheetHtml.Expand(source, Labels, "ru");

        Assert.Equal(
            [
                "<h3>Списания за период</h3>" +
                "<p>23.11.2026 – 29.11.2026</p>" +
                "<table bordered compact>" +
                "<tr><th align=\"left\">23 ноября, понедельник</th><th align=\"right\">8.5 ч</th></tr>" +
                "<tr><td><i>Проект</i> <b>Internal IT</b><br>b</td><td align=\"right\">8 ч</td></tr>" +
                "<tr><td><i>Проект</i> <b>Internal IT</b></td><td align=\"right\">0.5 ч</td></tr>" +
                "<tr><th align=\"left\">27 ноября, пятница</th><th align=\"right\">0.25 ч</th></tr>" +
                "<tr><td><i>Лид</i> <b>Test</b><br>a</td><td align=\"right\">0.25 ч</td></tr>" +
                "<tr><td><b>Итого за период</b></td><td align=\"right\"><b>8.75 ч</b></td></tr>" +
                "</table>"
            ],
            actual);
    }

    [Fact]
    public static void Expand_EnglishLocale_ExpectEnglishDates()
    {
        const string source =
            "<timesheets><day date=\"2026-11-27\"><entry hours=\"2\" type=\"Project\"><project>P</project></entry></day></timesheets>";

        var actual = AgentTimesheetHtml.Expand(source, new("Total", "Period total", "h"), "en");

        Assert.StartsWith("<p>27 November 2026, Friday</p>", actual[0], StringComparison.Ordinal);
        Assert.Contains("<b>2 h</b>", actual[0], StringComparison.Ordinal);
    }

    [Fact]
    public static void Expand_UserDataWithMarkup_ExpectEncodedTextAndLineBreaks()
    {
        const string source =
            "<timesheets><day date=\"2026-11-27\">" +
            "<entry hours=\"1\" type=\"Проект\"><project>R&amp;D <b>bold</b> & \"Co\"</project>" +
            "<comment>1 < 2 &lt;script&gt;\n  #Task8137 https://example.com/a?b=1&c=2</comment></entry>" +
            "</day></timesheets>";

        var actual = AgentTimesheetHtml.Expand(source, Labels, "ru");

        Assert.Contains(
            "<td><i>Проект</i> <b>R&amp;D bold &amp; &quot;Co&quot;</b><br>" +
            "1 &lt; 2 &lt;script&gt;<br>#Task8137 https://example.com/a?b=1&amp;c=2</td>",
            actual[0],
            StringComparison.Ordinal);
    }

    [Fact]
    public static void Expand_ExpandedHtml_ExpectAcceptedBySanitizerUnchanged()
    {
        const string source =
            "<h3>Списания за период</h3><timesheets from=\"2026-11-23\" to=\"2026-11-29\"><day date=\"2026-11-27\">" +
            "<entry hours=\"1\" type=\"Проект\"><project>R&D</project><comment>1 < 2\nвторая строка</comment></entry>" +
            "</day></timesheets>";

        var expanded = Assert.Single(AgentTimesheetHtml.Expand(source, Labels, "ru"));
        var prepared = AgentRichHtml.Prepare(expanded);

        Assert.True(prepared.IsRich);
        Assert.Equal(expanded, prepared.Html);
    }

    [Fact]
    public static void Expand_BlockWithoutEntries_ExpectBlockIsRemoved()
    {
        var actual = AgentTimesheetHtml.Expand("Списаний нет.<timesheets from=\"2026-11-23\" to=\"2026-11-29\"></timesheets>", Labels, "ru");

        Assert.Equal(["Списаний нет."], actual);
    }

    [Fact]
    public static void Expand_LongPeriod_ExpectSeveralMessagesWithinLimitsAndOneTotal()
    {
        var source = new StringBuilder("<h3>Списания за период</h3><timesheets from=\"2026-01-01\" to=\"2026-03-31\">");
        var date = new DateOnly(2026, 1, 1);

        for (var day = 0; day < 90; day++)
        {
            _ = source.Append("<day date=\"").Append(date.AddDays(day).ToString("yyyy-MM-dd")).Append("\">");
            for (var entry = 0; entry < 6; entry++)
            {
                _ = source.Append("<entry hours=\"1.5\" type=\"Проект\"><project>Project ").Append(entry).Append("</project><comment>")
                    .Append(new string('к', 60)).Append("</comment></entry>");
            }

            _ = source.Append("</day>");
        }

        var actual = AgentTimesheetHtml.Expand(source.Append("</timesheets>Конец").ToString(), Labels, "ru");

        Assert.True(actual.Count > 1);
        Assert.All(
            actual,
            static message =>
            {
                Assert.True(message.Length <= AgentTimesheetHtml.MaxLengthPerMessage + 200);
                Assert.True(message.Split("<tr>").Length - 1 <= AgentTimesheetHtml.MaxRowsPerMessage);
                Assert.Equal(message.Split("<table").Length, message.Split("</table>").Length);
            });

        Assert.StartsWith("<h3>Списания за период</h3><p>01.01.2026 – 31.03.2026</p><table", actual[0], StringComparison.Ordinal);
        Assert.Equal(540, actual.Sum(static message => message.Split("<i>Проект</i>").Length - 1));
        Assert.Equal(1, actual.Sum(static message => message.Split("Итого за период").Length - 1));
        Assert.EndsWith("<b>810 ч</b></td></tr></table>Конец", actual[^1], StringComparison.Ordinal);
    }

    [Fact]
    public static void SplitFallback_LongText_ExpectPartsWithinLimitAndNoLostLines()
    {
        var lines = Enumerable.Range(1, 300).Select(static index => $"<b>Строка {index}</b> — текст & текст").ToArray();

        var actual = AgentRichHtml.SplitFallback(string.Join('\n', lines), maxLength: 1000);

        Assert.True(actual.Count > 1);
        Assert.All(actual, static part => Assert.True(part.Length <= 1000));
        Assert.Equal(lines, string.Join('\n', actual).Split('\n'));
    }

    private const string WeekJson =
        """
        {"timesheets":[
          {"projectType":"Project","projectName":"X5","duration":6,"description":"#OutstaffWork","date":"2026-11-26T00:00:00"},
          {"projectType":"Lead","projectName":"Test","duration":1.5,"date":"2026-11-26"},
          {"projectType":"Project","projectName":"X5","duration":7,"description":"#OutstaffWork","date":"2026-11-27"}]}
        """;

    private const string LossyDay =
        "<day date=\"2026-11-26\"><entry hours=\"6\" type=\"Проект\"><project>X5</project></entry></day>";

    private const string RestoredDay26 =
        "\n<day date=\"2026-11-26\">" +
        "\n<entry hours=\"6\" type=\"Project\"><project>X5</project><comment>#OutstaffWork</comment></entry>" +
        "\n<entry hours=\"1.5\" type=\"Lead\"><project>Test</project></entry>" +
        "\n</day>";

    private const string RestoredDay27 =
        "\n<day date=\"2026-11-27\">" +
        "\n<entry hours=\"7\" type=\"Project\"><project>X5</project><comment>#OutstaffWork</comment></entry>" +
        "\n</day>";

    private static AgentTimesheetSnapshot CreateSnapshot(string from, string to, string json = WeekJson)
    {
        var snapshot = new AgentTimesheetSnapshot();
        snapshot.Remember([new("dateFrom", from), new("dateTo", to)], json);

        return snapshot;
    }

    [Fact]
    public static void Restore_PeriodBlockWithLostEntryAndLostDay_ExpectEntriesOfTheToolResult()
    {
        var actual = AgentTimesheetHtml.Restore(
            $"Текст <timesheets from=\"2026-11-23\" to=\"2026-11-29\">{LossyDay}</timesheets> конец",
            CreateSnapshot("2026-11-23", "2026-11-29"));

        Assert.Equal(
            $"Текст <timesheets from=\"2026-11-23\" to=\"2026-11-29\">{RestoredDay26}{RestoredDay27}\n</timesheets> конец", actual);
    }

    [Fact]
    public static void Restore_DayBlock_ExpectOnlyTheDayOfTheBlockIsRestored()
    {
        var actual = AgentTimesheetHtml.Restore($"<timesheets>{LossyDay}</timesheets>", CreateSnapshot("2026-11-23", "2026-11-29"));

        Assert.Equal($"<timesheets>{RestoredDay26}\n</timesheets>", actual);
    }

    [Fact]
    public static void Restore_PartialBlock_ExpectBlockIsKept()
    {
        var source = $"<timesheets partial from=\"2026-11-23\" to=\"2026-11-29\">{LossyDay}</timesheets>";

        Assert.Equal(source, AgentTimesheetHtml.Restore(source, CreateSnapshot("2026-11-23", "2026-11-29")));
    }

    [Fact]
    public static void Restore_DayIsNotReturnedByTools_ExpectModelDayIsKeptNextToRestoredOne()
    {
        const string otherDay = "<day date=\"2026-11-20\"><entry hours=\"1\" type=\"Проект\"><project>Old</project></entry></day>";

        var actual = AgentTimesheetHtml.Restore(
            $"<timesheets from=\"2026-11-20\" to=\"2026-11-26\">{LossyDay}{otherDay}</timesheets>", CreateSnapshot("2026-11-26", "2026-11-26"));

        Assert.Equal($"<timesheets from=\"2026-11-20\" to=\"2026-11-26\">\n{otherDay}{RestoredDay26}\n</timesheets>", actual);
    }

    [Theory]
    [InlineData("2026-11-01", "2026-11-02", WeekJson)]
    [InlineData("2026-11-23", "2026-11-29", """{"isSuccess":false}""")]
    [InlineData("2026-11-23", "2026-11-29", "not json")]
    [InlineData("2026-11-29", "2026-11-23", WeekJson)]
    [InlineData("2026-11-23", "2026-11-29", """{"timesheets":[{"duration":1}]}""")]
    public static void Restore_ToolsDoNotDescribeTheDayOfTheBlock_ExpectBlockIsKept(string from, string to, string json)
    {
        var source = $"<timesheets>{LossyDay}</timesheets>";

        Assert.Equal(source, AgentTimesheetHtml.Restore(source, CreateSnapshot(from, to, json)));
    }

    [Fact]
    public static void Restore_ToolsSayTheDayIsEmpty_ExpectBlockIsKept()
    {
        var source = $"<timesheets>{LossyDay}</timesheets>";

        Assert.Equal(source, AgentTimesheetHtml.Restore(source, CreateSnapshot("2026-11-26", "2026-11-26", """{"timesheets":[]}""")));
    }

    [Fact]
    public static void Expand_TypeNameOfTheService_ExpectLocalizedTypeAndModelTextAsItIs()
    {
        var labels = Labels with { LocalizeType = static type => type is "Lead" ? "Лид" : type };

        var actual = AgentTimesheetHtml.Expand(
            "<timesheets><day date=\"2026-11-27\">" +
            "<entry hours=\"1\" type=\"Lead\"><project>A</project></entry>" +
            "<entry hours=\"1\" type=\"Сделка\"><project>B</project></entry>" +
            "</day></timesheets>",
            labels,
            "ru");

        Assert.Contains("<i>Лид</i> <b>A</b>", Assert.Single(actual));
        Assert.Contains("<i>Сделка</i> <b>B</b>", actual[0]);
    }

    private const string MonthJson =
        """
        {"timesheets":[
          {"date":"2026-11-02","duration":4,"projectType":"Project","projectName":"X5","description":"#Work"},
          {"date":"2026-11-02","duration":1.5,"projectType":"Lead","projectName":"R&D <test>","description":"Call"},
          {"date":"2026-11-03","duration":8,"projectType":"Project","projectName":"X5","description":"#Work"},
          {"date":"2026-11-05","duration":0.25,"projectType":"Lead","projectName":"R&D <test>"}
        ]}
        """;

    [Fact]
    public static void Restore_SummaryByProject_ExpectHoursAreSummedFromToolResultInDescendingOrder()
    {
        var actual = AgentTimesheetHtml.Restore(
            "<h3>Часы</h3><summary by=\"project\" from=\"2026-11-01\" to=\"2026-11-30\"></summary>",
            CreateSnapshot("2026-11-01", "2026-11-30", MonthJson));

        Assert.Equal(
            "<h3>Часы</h3><summary by=\"project\" from=\"2026-11-01\" to=\"2026-11-30\">" +
            "\n<row hours=\"12\" type=\"Project\">X5</row>" +
            "\n<row hours=\"1.75\" type=\"Lead\">R&amp;D &lt;test&gt;</row>" +
            "\n</summary>",
            actual);
    }

    [Fact]
    public static void Restore_SummaryByDay_ExpectOneRowForEveryDayWithEntriesAndModelRowsAreDropped()
    {
        var actual = AgentTimesheetHtml.Restore(
            "<summary by=\"day\" from=\"2026-11-01\" to=\"2026-11-04\"><row hours=\"100\" date=\"2026-11-02\"></row></summary>",
            CreateSnapshot("2026-11-01", "2026-11-30", MonthJson));

        Assert.Equal(
            "<summary by=\"day\" from=\"2026-11-01\" to=\"2026-11-04\">" +
            "\n<row hours=\"5.5\" date=\"2026-11-02\"></row>" +
            "\n<row hours=\"8\" date=\"2026-11-03\"></row>" +
            "\n</summary>",
            actual);
    }

    [Fact]
    public static void Restore_SummaryByTypeAsSelfClosedTag_ExpectRowsByType()
    {
        var actual = AgentTimesheetHtml.Restore(
            "<summary by=\"type\" from=\"2026-11-01\" to=\"2026-11-30\"/>", CreateSnapshot("2026-11-01", "2026-11-30", MonthJson));

        Assert.Equal(
            "<summary by=\"type\" from=\"2026-11-01\" to=\"2026-11-30\">" +
            "\n<row hours=\"12\" type=\"Project\"></row>\n<row hours=\"1.75\" type=\"Lead\"></row>\n</summary>",
            actual);
    }

    [Theory]
    [InlineData("<summary by=\"project\" from=\"2026-10-01\" to=\"2026-10-31\"><row hours=\"7\" type=\"Project\">X5</row></summary>")]
    [InlineData("<summary by=\"week\" from=\"2026-11-01\" to=\"2026-11-30\"><row hours=\"7\">X5</row></summary>")]
    [InlineData("<summary by=\"project\"><row hours=\"7\">X5</row></summary>")]
    public static void Restore_SummaryCannotBeCalculated_ExpectModelRowsAreDroppedAndBlockIsNotShown(string source)
    {
        var restored = AgentTimesheetHtml.Restore("Текст " + source, CreateSnapshot("2026-11-01", "2026-11-30", MonthJson));

        Assert.DoesNotContain("<row", restored);
        Assert.Equal(["Текст "], AgentTimesheetHtml.Expand(restored, Labels, "ru"));
    }

    [Fact]
    public static void Restore_SummaryWithoutToolResult_ExpectModelRowsAreDropped()
    {
        var restored = AgentTimesheetHtml.Restore(
            "<summary by=\"day\" from=\"2026-11-01\" to=\"2026-11-30\"><row hours=\"7\" date=\"2026-11-02\"></row></summary>",
            new AgentTimesheetSnapshot());

        Assert.Equal("<summary by=\"day\" from=\"2026-11-01\" to=\"2026-11-30\">\n</summary>", restored);
    }

    [Fact]
    public static void Expand_SummaryByProject_ExpectPeriodTableWithLocalizedTypesEncodedNamesAndTotal()
    {
        var labels = Labels with { LocalizeType = static type => type is "Lead" ? "Лид" : type is "Project" ? "Проект" : type };

        var restored = AgentTimesheetHtml.Restore(
            "<h3>Часы по проектам</h3><summary by=\"project\" from=\"2026-11-01\" to=\"2026-11-30\"></summary>",
            CreateSnapshot("2026-11-01", "2026-11-30", MonthJson));

        var actual = AgentTimesheetHtml.Expand(restored, labels, "ru");

        Assert.Equal(
            [
                "<h3>Часы по проектам</h3><p>01.11.2026 – 30.11.2026</p><table bordered compact>" +
                "<tr><td><i>Проект</i> <b>X5</b></td><td align=\"right\">12\u00A0ч</td></tr>" +
                "<tr><td><i>Лид</i> <b>R&amp;D &lt;test&gt;</b></td><td align=\"right\">1.75\u00A0ч</td></tr>" +
                "<tr><td><b>Итого за период</b></td><td align=\"right\"><b>13.75\u00A0ч</b></td></tr>" +
                "</table>"
            ],
            actual);
    }

    [Fact]
    public static void Expand_SummaryByDay_ExpectDaysWithDayOfWeekAndTotal()
    {
        var restored = AgentTimesheetHtml.Restore(
            "<summary by=\"day\" from=\"2026-11-01\" to=\"2026-11-30\"></summary>", CreateSnapshot("2026-11-01", "2026-11-30", MonthJson));

        var actual = Assert.Single(AgentTimesheetHtml.Expand(restored, Labels, "ru"));

        Assert.Contains($"<tr><td>{AgentDateFormat.FormatDay(new(2026, 11, 2), "ru")}</td><td align=\"right\">5.5\u00A0ч</td></tr>", actual);
        Assert.Contains($"<tr><td>{AgentDateFormat.FormatDay(new(2026, 11, 5), "ru")}</td><td align=\"right\">0.25\u00A0ч</td></tr>", actual);
        Assert.EndsWith("<tr><td><b>Итого за период</b></td><td align=\"right\"><b>13.75\u00A0ч</b></td></tr></table>", actual);
    }
}
