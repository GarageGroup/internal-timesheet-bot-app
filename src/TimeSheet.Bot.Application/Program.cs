#nullable enable

using System.Threading.Tasks;

namespace GGroupp.Internal.Timesheet.Bot
{
    static class Program
    {
        static async Task Main(string[] args) => await BotApplication.RunAsync(args);
    }
}