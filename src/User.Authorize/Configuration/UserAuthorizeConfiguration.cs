namespace GGroupp.Internal.Timesheet;

public sealed record class UserAuthorizeConfiguration
{
    public UserAuthorizeConfiguration(string oAuthConnectionName)
        =>
        OAuthConnectionName = oAuthConnectionName ?? string.Empty;

    public string OAuthConnectionName { get; }
}