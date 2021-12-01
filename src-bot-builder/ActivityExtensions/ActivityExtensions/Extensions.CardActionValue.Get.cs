using System;
using Microsoft.Bot.Schema;

namespace GGroupp.Infra.Bot.Builder;

partial class ActivityExtensions
{
    public static Optional<Guid> GetCardActionValueOrAbsent(this Activity activity)
        =>
        InnerGetCardActionValueOrAbsent(
            activity ?? throw new ArgumentNullException(nameof(activity)));

    private static Optional<Guid> InnerGetCardActionValueOrAbsent(this Activity activity)
        =>
        activity.InnerIsMessageType() ? activity.ParseCardActionValueOrAbsent() : default;

    private static Optional<Guid> ParseCardActionValueOrAbsent(this Activity activity)
        =>
        Pipeline.Pipe(
            activity.Value is not null ? activity.Value.ToString() : activity.Text)
        .Pipe(
            CardActionValueJson.DeserializeOrAbsent)
        .Map(
            valueJson => valueJson.Id);
}