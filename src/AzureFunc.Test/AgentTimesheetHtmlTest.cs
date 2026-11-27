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
}
