using Xunit;

namespace GarageGroup.Internal.Timesheet.Test;

public static class AgentRichHtmlTest
{
    [Fact]
    public static void Prepare_TextWithInlineTagsOnly_ExpectRegularMessageWithLineBreaksKept()
    {
        var actual = AgentRichHtml.Prepare("<b>Итого</b>: 2 ч\nПроект <code>R&D</code> & <script>alert(1)</script>");

        Assert.False(actual.IsRich);
        Assert.Equal("<b>Итого</b>: 2 ч\nПроект <code>R&amp;D</code> &amp; &lt;script&gt;alert(1)&lt;/script&gt;", actual.Html);
    }

    [Fact]
    public static void Prepare_EncodedEntitiesFromModel_ExpectNoDoubleEncoding()
    {
        var actual = AgentRichHtml.Prepare("R&amp;D &lt;test&gt;");

        Assert.Equal("R&amp;D &lt;test&gt;", actual.Html);
    }

    [Fact]
    public static void Prepare_AnswerWithTable_ExpectRichMessageWithLooseTextInParagraphs()
    {
        const string source =
            "<b>Списание создано</b>\n\n" +
            "<h4>27 ноября 2026, пятница · 2 ч</h4>\n" +
            "<table bordered striped compact>\n" +
            "<tr><th>№</th><th align=\"left\">Проект</th><th align=\"right\">Часы</th></tr>\n" +
            "<tr><td>1</td><td>Проект <code>E2E Role test</code></td><td align=\"right\">2</td></tr>\n" +
            "</table>\n" +
            "Итого: 2 ч\nВсё верно?";

        var actual = AgentRichHtml.Prepare(source);

        Assert.True(actual.IsRich);
        Assert.Equal(
            "<p><b>Списание создано</b></p>" +
            "<h4>27 ноября 2026, пятница &#183; 2 ч</h4>" +
            "<table bordered striped compact> " +
            "<tr><th>№</th><th align=\"left\">Проект</th><th align=\"right\">Часы</th></tr> " +
            "<tr><td>1</td><td>Проект <code>E2E Role test</code></td><td align=\"right\">2</td></tr> " +
            "</table>" +
            "<p>Итого: 2 ч<br>Всё верно?</p>",
            actual.Html);
    }

    [Theory]
    [InlineData("<table onclick=\"x()\"><tr><td>1</td></tr></table>", "&lt;table onclick=&quot;x()&quot;&gt;")]
    [InlineData("<p style=\"color:red\">text</p>", "&lt;p style=&quot;color:red&quot;&gt;")]
    [InlineData("<td align=\"justify\">1</td>", "&lt;td align=&quot;justify&quot;&gt;")]
    [InlineData("<a href=\"https://example.com\">link</a>", "&lt;a href=&quot;https://example.com&quot;&gt;link&lt;/a&gt;")]
    [InlineData("<img src=\"x\">", "&lt;img src=&quot;x&quot;&gt;")]
    [InlineData("<b class=\"x\">bold</b>", "&lt;b class=&quot;x&quot;&gt;bold</b>")]
    public static void Prepare_TagOrAttributeIsNotAllowed_ExpectItIsShownAsText(string source, string expectedPart)
    {
        var actual = AgentRichHtml.Prepare(source);

        Assert.Contains(expectedPart, actual.Html);
    }

    [Fact]
    public static void Prepare_AllowedAttributes_ExpectNormalizedTags()
    {
        var actual = AgentRichHtml.Prepare("<TABLE Compact><tr><TD ALIGN=\"RIGHT\" colspan=\"2\">1</TD></tr></TABLE><details open><summary>S</summary>T</details>");

        Assert.Equal(
            "<table compact><tr><td align=\"right\" colspan=\"2\">1</td></tr></table><details open><summary>S</summary>T</details>",
            actual.Html);
    }

    [Fact]
    public static void Prepare_List_ExpectRichMessage()
    {
        var actual = AgentRichHtml.Prepare("Найдено несколько проектов:\n<ol><li>Проект <code>A</code></li><li>Лид <code>B</code></li></ol>");

        Assert.True(actual.IsRich);
        Assert.Equal("<p>Найдено несколько проектов:</p><ol><li>Проект <code>A</code></li><li>Лид <code>B</code></li></ol>", actual.Html);
    }

    [Fact]
    public static void Prepare_TimeEntriesAsTwoLineList_ExpectRichMessageAndNumberedFallback()
    {
        const string source =
            "<h3>23.11.2026 – 29.11.2026 · 3.5 ч</h3>\n" +
            "<p><b>27 ноября 2026, пятница · 3.5 ч</b></p>\n" +
            "<ol>\n" +
            "<li><b>2 ч</b> · <i>Проект</i> <b>E2E Role test</b><br>#Test</li>\n" +
            "<li><b>1.5 ч</b> · <i>Сделка</i> <b>AMBY</b><br>Тестирую</li>\n" +
            "</ol>";

        var actual = AgentRichHtml.Prepare(source);

        Assert.True(actual.IsRich);
        Assert.Contains("<li><b>2 ч</b> &#183; <i>Проект</i> <b>E2E Role test</b><br>#Test</li>", actual.Html);
        Assert.DoesNotContain("\n", actual.Html);

        Assert.Equal(
            "<b>23.11.2026 – 29.11.2026 &#183; 3.5 ч</b>\n\n" +
            "<b>27 ноября 2026, пятница &#183; 3.5 ч</b>\n\n" +
            "1. <b>2 ч</b> &#183; <i>Проект</i> <b>E2E Role test</b>\n#Test\n" +
            "2. <b>1.5 ч</b> &#183; <i>Сделка</i> <b>AMBY</b>\nТестирую",
            AgentRichHtml.ToFallback(actual.Html));
    }

    [Fact]
    public static void ToFallback_RichAnswer_ExpectRegularHtmlWithRowsAsLines()
    {
        const string rich =
            "<p><b>Списание создано</b></p>" +
            "<h4>27 ноября 2026, пятница</h4>" +
            "<table bordered striped compact>" +
            "<tr><th>№</th><th align=\"left\">Проект</th><th align=\"right\">Часы</th></tr>" +
            "<tr><td>1</td><td>Проект <code>E2E</code></td><td align=\"right\">2</td></tr>" +
            "</table>" +
            "<ol><li>Первый</li><li>Второй</li></ol>" +
            "<ul><li>Пункт</li></ul>" +
            "<p>Итого: 2 ч<br>Готово</p>";

        var actual = AgentRichHtml.ToFallback(rich);

        Assert.Equal(
            "<b>Списание создано</b>\n\n" +
            "<b>27 ноября 2026, пятница</b>\n\n" +
            "№ — Проект — Часы\n" +
            "1 — Проект <code>E2E</code> — 2\n\n" +
            "1. Первый\n" +
            "2. Второй\n\n" +
            "• Пункт\n\n" +
            "Итого: 2 ч\nГотово",
            actual);

        // The fallback contains only tags of a regular Telegram message
        Assert.DoesNotMatch("</?(?!b>|i>|code>)[a-z]", actual);
    }
}
