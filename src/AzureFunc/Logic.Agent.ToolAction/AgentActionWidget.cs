using System;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Localization;

namespace GarageGroup.Internal.Timesheet;

using static AgentToolActionResource;

internal sealed record class AgentActionWidgetContent(string Html, string Caption);

// The confirmation card of a prepared time entry as a Telegram widget: the user changes the date and the duration
// with buttons and confirms the action. A widget has no keyboard input, so the other fields are shown as they are.
// The widget sends one query to the bot: "c:<action id>:<yyyyMMdd>:<quarters of an hour>" or "x:<action id>"
internal static class AgentActionWidget
{
    internal const string StatusConfirmed = "ok";

    internal const string StatusCancelled = "x";

    internal const string StatusNotFound = "n";

    internal const string StatusFailed = "e";

    // The action is kept and may be confirmed again, for example after the user signs in
    internal const string StatusRetry = "r";

    private const int MaxDurationQuarters = 96;

    private static readonly int[] QuickHours = [1, 2, 4, 8];

    private static readonly string[] RussianMonths =
    [
        "января", "февраля", "марта", "апреля", "мая", "июня", "июля", "августа", "сентября", "октября", "ноября", "декабря"
    ];

    private static readonly string[] RussianDays = ["воскресенье", "понедельник", "вторник", "среда", "четверг", "пятница", "суббота"];

    private static readonly string[] EnglishMonths =
    [
        "January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December"
    ];

    private static readonly string[] EnglishDays = ["Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday"];

    // Returns null when the action cannot be shown as an editable card: the regular card is used then
    internal static AgentActionWidgetContent? TryBuild(AgentPendingAction action, IStringLocalizer localizer, string locale, DateOnly today)
    {
        if (AgentActionEditor.IsEditable(action) is false)
        {
            return null;
        }

        var call = action.Calls[0];
        var reference = Array.Find(call.Arguments, static argument => argument.Details is { Length: > 0 });

        var oldDate = ParseDate(FindValue(reference?.Details, AgentActionEditor.DateArgumentName));
        var oldQuarters = ParseQuarters(FindValue(reference?.Details, AgentActionEditor.DurationArgumentName));

        var date = ParseDate(FindValue(call.Arguments, AgentActionEditor.DateArgumentName)) ?? oldDate;
        var quarters = ParseQuarters(FindValue(call.Arguments, AgentActionEditor.DurationArgumentName)) ?? oldQuarters;

        // The earliest date that may be in an open period: the first day of the previous month
        var minDate = new DateOnly(today.Year, today.Month, 1).AddMonths(-1);
        if (date is null || quarters is null || date > today || date < minDate)
        {
            return null;
        }

        var isRussian = locale.StartsWith("ru", StringComparison.OrdinalIgnoreCase);
        var config = new
        {
            a = action.Id,
            d = Format(date.Value),
            n = Format(minDate),
            x = Format(today),
            h = quarters.Value,
            od = oldDate is null ? null : Format(oldDate.Value),
            oh = oldQuarters,
            u = localizer[HoursUnit].Value,
            M = isRussian ? RussianMonths : EnglishMonths,
            D = isRussian ? RussianDays : EnglishDays,
            t = new
            {
                h = localizer[WidgetHint].Value,
                w = localizer[WidgetSending].Value,
                ok = localizer[WidgetConfirmed].Value,
                x = localizer[Cancelled].Value,
                n = localizer[NotFound].Value,
                e = localizer[WidgetFailed].Value,
                r = localizer[SignInRequired].Value
            }
        };

        // The script needs both editable rows in the card
        var rows = AgentToolActionPreview.GetRows(call, localizer, locale);
        if (rows.Count(static row => row.Name is AgentActionEditor.DateArgumentName) is not 1 ||
            rows.Count(static row => row.Name is AgentActionEditor.DurationArgumentName) is not 1)
        {
            return null;
        }

        var title = AgentToolActionPreview.GetTitle(call, localizer);
        var html = new StringBuilder(Style)
            .Append("<p><b>").Append(WebUtility.HtmlEncode(title)).Append("</b></p><table>");

        foreach (var row in rows)
        {
            _ = html.Append("<tr><th>");
            if (row.OldLabel is not null)
            {
                _ = html.Append("<s>").Append(WebUtility.HtmlEncode(row.OldLabel)).Append("</s><br>");
            }

            _ = html.Append(WebUtility.HtmlEncode(row.Label)).Append("</th><td>");
            _ = row.Name switch
            {
                AgentActionEditor.DateArgumentName => html.Append(DateCell),
                AgentActionEditor.DurationArgumentName => html.Append(DurationCell).Append(BuildQuickButtons(config.u)),
                _ => html
                    .Append(row.OldValue is null ? string.Empty : "<s>" + WebUtility.HtmlEncode(row.OldValue) + "</s><br>")
                    .Append(WebUtility.HtmlEncode(row.Value))
            };

            _ = html.Append("</td></tr>");
        }

        _ = html
            .Append("</table><p id=m></p><div class=f><button id=ok>")
            .Append(WebUtility.HtmlEncode(localizer[ConfirmButton]))
            .Append("</button><button id=no>")
            .Append(WebUtility.HtmlEncode(localizer[CancelButton]))
            .Append("</button></div><script>var C=")
            .Append(JsonSerializer.Serialize(config, ConfigSerializerOptions))
            .Append(';')
            .Append(Script)
            .Append("</script>");

        return new(html.ToString(), WebUtility.HtmlEncode(title));
    }

    // The state the widget shows after the action has been processed. The confirmed values are kept in the state,
    // so the card shows them when it is opened again
    internal static string BuildChatData(string status, AgentActionEdit? edit = null)
        =>
        JsonSerializer.Serialize(
            new
            {
                s = status,
                d = edit?.Date is DateOnly date ? Format(date) : null,
                h = edit?.Duration is decimal duration ? (int?)(duration / AgentActionEditor.DurationStep) : null
            },
            StateSerializerOptions);

    // The data comes from the code of the widget on the user's device, so every part of it is checked
    internal static bool TryParseQuery(string? data, out string actionId, out bool isConfirmed, out AgentActionEdit? edit)
    {
        (actionId, isConfirmed, edit) = (string.Empty, false, null);

        var parts = (data ?? string.Empty).Split(':');
        if (parts.Length < 2 || parts[1].Length is 0 || parts[1].All(char.IsAsciiLetterOrDigit) is false)
        {
            return false;
        }

        actionId = parts[1];
        if (parts is ["x", _])
        {
            return true;
        }

        if (parts is not ["c", _, var dateText, var quartersText] ||
            DateOnly.TryParseExact(dateText, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date) is false ||
            int.TryParse(quartersText, NumberStyles.None, CultureInfo.InvariantCulture, out var quarters) is false ||
            quarters is < 1 or > MaxDurationQuarters)
        {
            return false;
        }

        isConfirmed = true;
        edit = new(date, quarters * AgentActionEditor.DurationStep);

        return true;
    }

    private static string BuildQuickButtons(string unit)
    {
        var builder = new StringBuilder("<div class=\"c q\">");
        foreach (var hours in QuickHours)
        {
            _ = builder.Append("<button data-q=").Append(hours * 4).Append('>').Append(hours).Append("&nbsp;").Append(WebUtility.HtmlEncode(unit)).Append("</button>");
        }

        return builder.Append("</div>").ToString();
    }

    private static string? FindValue(AgentActionArgument[]? arguments, string name)
        =>
        arguments is null ? null : Array.Find(arguments, argument => argument.Name == name && argument.Details is null)?.Value;

    private static DateOnly? ParseDate(string? value)
    {
        var text = value?.Trim() ?? string.Empty;
        if (text.Length > AgentActionEditor.DateFormat.Length)
        {
            text = text[..AgentActionEditor.DateFormat.Length];
        }

        return DateOnly.TryParseExact(text, AgentActionEditor.DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)
            ? date
            : null;
    }

    private static int? ParseQuarters(string? value)
        =>
        decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var duration) && AgentActionEditor.IsValidDuration(duration)
            ? (int)(duration / AgentActionEditor.DurationStep)
            : null;

    private static string Format(DateOnly date)
        =>
        date.ToString(AgentActionEditor.DateFormat, CultureInfo.InvariantCulture);

    // Letters of any language are written as they are to keep the code small, and the characters of HTML markup are escaped
    private static readonly JsonSerializerOptions ConfigSerializerOptions = new()
    {
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.Create(System.Text.Unicode.UnicodeRanges.All)
    };

    private static readonly JsonSerializerOptions StateSerializerOptions = new()
    {
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    private const string Style =
        "<style>table{width:100%}th{text-align:left;white-space:nowrap;vertical-align:top;padding-right:8px}" +
        ".c{display:flex;gap:6px;align-items:center;flex-wrap:wrap;margin-bottom:4px}.c button{min-width:36px;padding:4px 8px}" +
        ".f{display:flex;gap:8px}.f button{flex:1}s,#m{color:var(--tg-theme-hint-color)}.z button{display:none}</style>";

    private const string DateCell = "<s id=do></s><div class=c><button id=dp>◀</button><span id=d></span><button id=dn>▶</button></div>";

    private const string DurationCell = "<s id=ho></s><div class=c><button id=hm>−</button><b id=h></b><button id=hp>+</button></div>";

    // d is the chosen date, h is the chosen duration in quarters of an hour, st is the state written by the bot
    private const string Script =
        "var W=window.TelegramWidget,d=C.d,h=C.h,st=null,busy=0,er='';" +
        "function $(i){return document.getElementById(i)}" +
        "function pd(s){var a=s.split('-');return new Date(Date.UTC(+a[0],a[1]-1,+a[2]))}" +
        "function fd(s){var t=pd(s);return t.getUTCDate()+' '+C.M[t.getUTCMonth()]+' '+t.getUTCFullYear()+', '+C.D[t.getUTCDay()]}" +
        "function fh(q){return q/4+'\\u00a0'+C.u}" +
        "function r(){var f=st&&st.s!='r';if(f){d=st.d||d;h=st.h||h}" +
        "$('d').textContent=fd(d);$('h').textContent=fh(h);" +
        "$('do').textContent=C.od&&C.od!=d?fd(C.od):'';$('ho').textContent=C.oh&&C.oh!=h?fh(C.oh):'';" +
        "document.body.className=f?'z':'';var e=!f&&!busy,b=document.querySelectorAll('button');" +
        "for(var i=0;i<b.length;i++)b[i].disabled=!e;" +
        "if(e){$('dp').disabled=d<=C.n;$('dn').disabled=d>=C.x;$('hm').disabled=h<=1;$('hp').disabled=h>=96}" +
        "$('m').textContent=f?C.t[st.s]||C.t.e:busy?C.t.w:er||(st?C.t.r:C.t.h)}" +
        "function sd(n){var t=pd(d);t.setUTCDate(t.getUTCDate()+n);var s=t.toISOString().slice(0,10);if(s>=C.n&&s<=C.x){d=s;r()}}" +
        "function sh(q){if(q>=1&&q<=96){h=q;r()}}" +
        "function rq(s){busy=1;er='';r();W.request(s).then(function(){},function(x){busy=0;er=C.t.e+' ('+(x&&x.code||'')+')';r()})}" +
        "function rs(){try{st=JSON.parse(W.chatData)}catch(x){st=null}if(st&&!st.s)st=null;if(st)busy=0;r()}" +
        "function on(i,f){$(i).addEventListener('click',f)}" +
        "on('dp',function(){sd(-1)});on('dn',function(){sd(1)});on('hm',function(){sh(h-1)});on('hp',function(){sh(h+1)});" +
        "var qb=document.querySelectorAll('[data-q]');" +
        "for(var i=0;i<qb.length;i++)qb[i].addEventListener('click',function(){sh(+this.getAttribute('data-q'))});" +
        "on('ok',function(){rq('c:'+C.a+':'+d.replace(/-/g,'')+':'+h)});on('no',function(){rq('x:'+C.a)});" +
        "W.onEvent('chatDataChanged',rs);rs();";
}
