using System;
using System.Collections.Generic;
using GarageGroup.Infra.Bot.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GarageGroup.Internal.Timesheet;

partial class Application
{
    private static IBotBuilder UseBotInfoFlow(this IBotBuilder botBuilder)
        =>
        botBuilder.UseBotInfo(BotInfoCommand, GetBotInfoData);

    private static BotInfoData GetBotInfoData(IBotContext botContext)
        =>
        new()
        {
            Values = botContext.ServiceProvider.GetRequiredService<IConfiguration>().GetRequiredSection("Info").GetBotInfoValue()
        };

    private static FlatArray<KeyValuePair<string, string?>> GetBotInfoValue(this IConfigurationSection section)
        =>
        [
            new("Name", section["ApiName"]),
            new("Description", section["Description"]),
            new("Version", section["ApiVersion"]),
            new("Build time", section.GetValue<DateTimeOffset?>("BuildDateTime").ToRussianStandardTimeZoneString())
        ];
}