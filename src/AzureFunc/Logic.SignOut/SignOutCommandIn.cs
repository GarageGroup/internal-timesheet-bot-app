using System;
using GarageGroup.Infra.Telegram.Bot;

namespace GarageGroup.Internal.Timesheet;

internal sealed record class SignOutCommandIn : IChatCommandIn<Unit>
{
    internal static readonly SignOutCommandIn Instance
        =
        new();

    public static string Type { get; } = "SignOut";
}