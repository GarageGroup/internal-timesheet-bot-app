using System;
using System.Linq;
using GGroupp.Infra.Bot.Builder;

namespace GGroupp.Internal.Timesheet;

partial class DataverseUserExtensions
{
    internal static Optional<Guid> GetDataverseUserIdOrAbsent(this BotUser user)
        =>
        user.Claims.GetValueOrAbsent(DataverseUserIdClaimName).Map(Guid.Parse);
}