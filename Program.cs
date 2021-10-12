using DSharpPlus.Entities;
using Serilog;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace EbayBot
{
    class Program
    {
        static async Task Main(string[] args)
        {
            //Setup the logger
            Log.Logger = new LoggerConfiguration().WriteTo.Async(x => x.File("Logs/log.log", retainedFileCountLimit: null, rollingInterval: RollingInterval.Day)).WriteTo.Console().CreateLogger();

            //Load config
            Config.LoadConfig();

            //Connect to Discord
            var discordThread = new Thread(Discord.MainAsync().GetAwaiter().GetResult);
            discordThread.Start();

            //Load Google Sheets
            await GoogleSheets.GoogleSheetsSetup();
            await GoogleSheets.GetTopTrumpsAndAddToDb();

            //Set up the yob scheduler
            await JobScheduler.SetupJobScheduler();

            await Task.Delay(-1);
        }
    }
}
