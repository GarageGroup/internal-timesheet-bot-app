using System.Collections.Generic;

namespace GarageGroup.Internal.Timesheet;

partial class CrmProjectApiExtensions
{
    internal static IEnumerable<T> NotNull<T>(this IEnumerable<T?> source)
        where T : class
    {
        foreach (var item in source)
        {
            if (item is not null)
            {
                yield return item;
            }
        }
    }
}