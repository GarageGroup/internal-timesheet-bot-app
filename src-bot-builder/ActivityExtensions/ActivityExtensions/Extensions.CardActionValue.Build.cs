using System;
using Microsoft.Bot.Schema;

namespace GGroupp.Infra.Bot.Builder;

partial class ActivityExtensions
{
    public static object BuildCardActionValue(this Activity activity, Guid valueId)
        =>
        InnerBuildCardActionValue(
            activity ?? throw new ArgumentNullException(nameof(activity)),
            new(valueId));

    private static object InnerBuildCardActionValue(Activity activity, CardActionValueJson valueJson)
        =>
        activity.InnerIsCardSupported() ? valueJson : valueJson.Serialize();
}