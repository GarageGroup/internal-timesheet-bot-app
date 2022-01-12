using System;

namespace GGroupp.Internal.Timesheet;

internal sealed record class UserAuthorizeConfigurationJson : IFunc<UserAuthorizeConfiguration>
{
    public string? OAuthConnectionName { get; init; }

    UserAuthorizeConfiguration IFunc<UserAuthorizeConfiguration>.Invoke()
        =>
        new(OAuthConnectionName ?? string.Empty);
}