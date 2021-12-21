using System;

namespace GGroupp.Internal.Timesheet;

public sealed record class UserAuthorizeConfiguration
{
    public UserAuthorizeConfiguration(string oAuthConnectionName, TimeSpan? oAuthTimeout)
    {
        OAuthConnectionName = oAuthConnectionName ?? string.Empty;
        OAuthTimeout = oAuthTimeout;
    }

    public string OAuthConnectionName { get; }

    public TimeSpan? OAuthTimeout { get; }
}