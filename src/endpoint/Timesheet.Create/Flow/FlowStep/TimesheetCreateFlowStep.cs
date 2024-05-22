using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Text.Json;

namespace GarageGroup.Internal.Timesheet;

internal static partial class TimesheetCreateFlowStep
{
    private const int DaysInRow = 3;

    private const int DaysRowsCount = 2;

    private const int MaxProjectsCount = 6;

    private const int ProjectDays = 30;

    private const int DescriptionTagDays = 30;

    private const decimal MaxDurationValue = 24;

    private const string DateFormat = "d MMMM yyyy";

    private static DateOnly GetToday()
        =>
        DateOnly.FromDateTime(DateTime.Now);

    private static DateOnly GetDateUtc(int daysAddedToNow)
        =>
        DateOnly.FromDateTime(DateTime.UtcNow.AddDays(daysAddedToNow));

    private static readonly JsonSerializerOptions SerializerOptions
        =
        new(JsonSerializerDefaults.Web);

    private static readonly FlatArray<CultureInfo> DurationParserCultures
        =
        [
            CultureInfo.GetCultureInfo("ru-RU"),
            CultureInfo.InvariantCulture
        ];

    private static readonly FlatArray<FlatArray<KeyValuePair<string, decimal>>> DurationSuggestions
        =
        [
            [new("0,25", 0.25m), new("0,5", 0.5m), new("0,75", 0.75m), new("1", 1)],
            [new("1,25", 1.25m), new("1,5", 1.5m), new("2", 2), new("2,5", 2.5m)],
            [new("3", 3), new("4", 4), new("6", 6), new("8", 8)]
        ];

    private static string CompressDataJson(this WebAppTimesheetCreateData data)
    {
        var json = JsonSerializer.Serialize(data, SerializerOptions);

        var buffer = Encoding.UTF8.GetBytes(json);
        var memoryStream = new MemoryStream();

        using (var zipStream = new GZipStream(memoryStream, CompressionMode.Compress, true))
        {
            zipStream.Write(buffer, 0, buffer.Length);
        }

        return Convert.ToBase64String(memoryStream.ToArray());
    }
}