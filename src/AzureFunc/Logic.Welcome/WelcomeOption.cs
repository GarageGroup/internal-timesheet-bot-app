using System;

namespace GarageGroup.Internal.Timesheet;

internal sealed record class WelcomeOption
{
    public WelcomeOption(string imageUrl)
        =>
        ImageUrl = imageUrl.OrEmpty();

    public string ImageUrl { get; }

    public FlatArray<string> Emojis { get; init; }
}