using System;
using System.Threading;
using System.Threading.Tasks;

namespace GarageGroup.Internal.Timesheet;

internal sealed record class EntraTokenSet(string AccessToken, string? RefreshToken, TimeSpan ExpiresIn);

// IsGrantInvalid is true when the authorization code or the refresh token is no longer accepted, so the user must sign in again
internal sealed class EntraTokenException(string message, bool isGrantInvalid, Exception? innerException = null)
    : Exception(message, innerException)
{
    public bool IsGrantInvalid { get; } = isGrantInvalid;
}

internal interface IEntraTokenClient
{
    ValueTask<EntraTokenSet> RedeemCodeAsync(string code, string codeVerifier, string scope, CancellationToken cancellationToken);

    ValueTask<EntraTokenSet> RefreshAsync(string refreshToken, string scope, CancellationToken cancellationToken);
}
