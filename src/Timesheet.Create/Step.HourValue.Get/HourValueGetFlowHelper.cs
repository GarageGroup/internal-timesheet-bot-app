using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace GGroupp.Internal.Timesheet;

internal static class HourValueGetExtensions
{
    private static readonly IReadOnlyCollection<CultureInfo> AwailableCultures;

    static HourValueGetExtensions()
        =>
        AwailableCultures = new[]
        {
            CultureInfo.GetCultureInfo("ru-RU"),
            CultureInfo.InvariantCulture
        };

    internal static Result<decimal, Unit> ParseHourValueOrFailure(this string? text)
        =>
        AwailableCultures.Select(text.ParseWithCultureOrFailure).FirstOrDefault(r => r.IsSuccess);

    private static Result<decimal, Unit> ParseWithCultureOrFailure(this string? text, CultureInfo culture)
        =>
        decimal.TryParse(text, NumberStyles.Number, culture, out var value) ? Result.Present(value) : default;
}