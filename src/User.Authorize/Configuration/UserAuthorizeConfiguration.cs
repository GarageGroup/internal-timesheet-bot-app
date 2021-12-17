namespace GGroupp.Internal.Timesheet;

public sealed record class UserAuthorizeConfiguration
{
    public UserAuthorizeConfiguration(string oAuthConnectionName, int? oAuthTimeoutMilliseconds)
    {
        OAuthConnectionName = oAuthConnectionName ?? string.Empty;
        OAuthTimeoutMilliseconds = oAuthTimeoutMilliseconds;
    }

    public string OAuthConnectionName { get; }

    public int? OAuthTimeoutMilliseconds { get; }
}