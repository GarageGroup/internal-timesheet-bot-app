using System.Linq;
using GGroupp.Infra.Bot.Builder;

namespace GGroupp.Internal.Timesheet;

partial class DataverseUserExtensions
{
    internal static string GetUserName(this BotUser user)
        =>
        string.IsNullOrEmpty(user.DisplayName) is false
        ? user.DisplayName
        : user.Claims.GetValueOrAbsent(DataverseUserFirstNameClaimName).OrElse(string.Empty);
}