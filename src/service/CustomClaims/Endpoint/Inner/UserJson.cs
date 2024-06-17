using System;
using System.Text.Json.Serialization;
using GarageGroup.Infra;

namespace GarageGroup.Internal.Timesheet.Inner;

internal record struct UserJson
{
    private const string EntityPluralName = "systemusers";

    private const string SystemUserIdFieldName = "systemuserid";
    
    private const string ActiveDirectoryUserIdFieldName = "azureactivedirectoryobjectid";

    internal static DataverseEntityGetIn BuildGetInput(Guid activeDirectoryUserId)
        => 
        new(
            entityPluralName: EntityPluralName,
            selectFields: [SystemUserIdFieldName],
            entityKey: new DataverseAlternateKey(
                ActiveDirectoryUserIdFieldName, 
                activeDirectoryUserId.ToString()));

    [JsonPropertyName(SystemUserIdFieldName)]
    public required Guid Id { get; init; }
}