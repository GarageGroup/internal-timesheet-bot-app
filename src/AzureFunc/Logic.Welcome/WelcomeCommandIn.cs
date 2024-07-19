using System;
using GarageGroup.Infra.Telegram.Bot;

namespace GarageGroup.Internal.Timesheet;

internal sealed record class WelcomeCommandIn : IChatCommandIn<Unit>
{
    internal static readonly WelcomeCommandIn Instance
        =
        new();

    public static string Type { get; } = "Welcome";
}