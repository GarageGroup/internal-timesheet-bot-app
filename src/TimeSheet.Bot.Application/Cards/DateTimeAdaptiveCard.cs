#nullable enable

using System;

namespace GGroupp.Internal.Timesheet.Bot
{
    internal static class DateTimeAdaptiveCard
    {
        public static AdaptiveCardJson Create(DateTimeOffset value)
            =>
            new("1.3")
            {
                Body = new object[]
                {
                    new
                    {
                        type = "TextBlock",
                        text = "Введите дату"
                    },
                    new
                    {
                        type = "Input.Date",
                        id =  "date",
                        placeholder = "Введите дату",
                        value = value.ToString("yyyy-MM-dd")
                    }
                },
                Actions = new object[]
                {
                    new
                    {
                        type = "Action.Submit",
                        title = "Подтвердить выбор"
                    }
                }
            };
    }
}