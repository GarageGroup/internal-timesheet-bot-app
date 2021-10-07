#nullable enable

using System;
using System.Linq;
using System.Text.Json;
using Microsoft.Bot.Builder.Dialogs;

namespace GGroupp.Internal.Timesheet.Bot
{
    internal static class WaterfallExtensions
    {
        private const string WaterfallCurrentValueParameterName = "WaterfallCurrentValue";

        public static T GetWaterfallCurrentValue<T>(this WaterfallStepContext stepContext)
            where T : new()
        {
            var json = stepContext.Values.GetValueOrAbsent(WaterfallCurrentValueParameterName).OrDefault()?.ToString();
            if(string.IsNullOrEmpty(json))
            {
                return new();
            }
            return JsonSerializer.Deserialize<T>(json) ?? new();
        }

        public static Unit SaveWaterfallCurrentValue<T>(this WaterfallStepContext stepContext, T value)
        {
            stepContext.Values[WaterfallCurrentValueParameterName] = JsonSerializer.Serialize(value);
            return default;
        }
    }
}