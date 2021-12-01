using System;
using System.Collections.Generic;
using System.Linq;

namespace GGroupp.Infra.Bot.Builder;

partial class LookupActivity
{
    internal static ChatFlowAction<LookupValue> ToAwaitAndRetryWithLookupCacheAction(this LookupValueSetSeachOut searchOut)
    {
        var items = searchOut.Items.ToDictionary(
            i => i.Id,
            i => new LookupCacheValueJson { Name = i.Name, Extensions = i.Extensions.ToArray() });

        if (items.Any() is false)
        {
            items = null;
        }

        return ChatFlowAction.AwaitAndRetry<LookupValue>(items);
    }

    private static Optional<LookupValue> GetFromLookupCacheOrAbsent(this IStepStateSupplier flowContext, Guid id)
    {
        return GetLookupCacheOrAbsent().FlatMap(GetLookupValueOrAbsent);

        Optional<Dictionary<Guid, LookupCacheValueJson>> GetLookupCacheOrAbsent()
            =>
            flowContext.StepState is Dictionary<Guid, LookupCacheValueJson> cache ? Optional.Present(cache) : default;

        Optional<LookupValue> GetLookupValueOrAbsent(Dictionary<Guid, LookupCacheValueJson> cacheItems)
            =>
            cacheItems.GetValueOrAbsent(id).Map(CreateItem);

        LookupValue CreateItem(LookupCacheValueJson cacheValueJson)
            =>
            new(id, cacheValueJson.Name, cacheValueJson.Extensions);
    }
}