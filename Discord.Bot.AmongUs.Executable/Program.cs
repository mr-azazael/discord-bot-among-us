using Discord.Bot.AmongUs.Library;
using System;
using System.Threading.Tasks;

namespace Discord.Bot.AmongUs.Executable
{
    class Program
    {
        static void Main(string[] args)
        {
            var bot = RunBot();
            Console.ReadLine();
            bot.Result.DisconnectAsync();
        }

        static async Task<BotAmongUs> RunBot()
        {
            var configuration = await JsonConfiguration.LoadConfiguration("configuration.json");
            var botAmongUs = new BotAmongUs(configuration);
            await botAmongUs.ConnectAsync();
            return botAmongUs;
        }
    }
}
