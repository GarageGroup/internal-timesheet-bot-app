using GarageGroup.Infra.Telegram.Bot;
using Xunit;

namespace GarageGroup.Internal.Timesheet;

public static class AgentProfileCommandTest
{
    [Fact]
    public static void Command_ExpectNotFallbackParser()
    {
        var parserType = typeof(IChatCommandParser<AgentProfileCommandIn>);

        Assert.False(parserType.IsAssignableFrom(typeof(AgentProfileCommand)));
    }
}
