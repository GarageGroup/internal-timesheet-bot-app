using System;

namespace GGroupp.Infra.Bot.Builder;

internal static class InternalPipeline
{
    internal static TResult InternalPipe<T, TResult>(this T source, Func<T, TResult> pipe)
        =>
        pipe.Invoke(source);
}