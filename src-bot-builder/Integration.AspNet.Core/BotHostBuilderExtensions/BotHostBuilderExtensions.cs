using System;

namespace Microsoft.Extensions.Hosting;

public static partial class BotHostBuilderExtensions
{
    private static TResult InnerPipe<T, TResult>(this T item, Func<T, TResult> pipe)
        =>
        pipe.Invoke(item);
}