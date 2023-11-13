using System.Collections.Generic;
using System.Linq;

namespace GarageGroup.Internal.Timesheet;

partial class CrmProjectApiExtensions
{
    internal static IEnumerable<T> TakeTop<T>(this IEnumerable<T> source, int? count)
        =>
        count is not null ? source.Take(count.Value) : source;
}