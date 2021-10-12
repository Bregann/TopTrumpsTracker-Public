using eBay.ApiClient.Auth.OAuth2;
using eBay.ApiClient.Auth.OAuth2.Model;
using Quartz;
using Quartz.Impl;
using Serilog;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EbayBot
{
    public class JobScheduler
    {
        private static StdSchedulerFactory _factory;
        private static IScheduler _scheduler;

        public static async Task SetupJobScheduler()
        {
            //construct the scheduler factory
            _factory = new StdSchedulerFactory();
            _scheduler = await _factory.GetScheduler();
            await _scheduler.Start();

            var syncSpreadsheetTrigger = TriggerBuilder.Create().WithIdentity("syncSpreadsheetTrigger").WithCronSchedule("0 0 2 1/1 * ? *").Build();
            var syncSpreadsheet = JobBuilder.Create<SyncSpreadsheetJob>().WithIdentity("syncSpreadsheet").Build();

            var apiRefreshTrigger = TriggerBuilder.Create().WithIdentity("apiRefreshTrigger").WithCronSchedule("0 55 * ? * * *").Build();
            var apiRefresh = JobBuilder.Create<RefreshTokenJob>().WithIdentity("apiRefresh").Build();

            var checkEbayForTopTrumps = JobBuilder.Create<CheckEbayJob>().StoreDurably().WithIdentity("checkEbayForTopTrumps").Build();
            await _scheduler.AddJob(checkEbayForTopTrumps, true);

            var checkEbayForTopTrumpsTrigger = TriggerBuilder.Create().WithIdentity("checkEbayForTopTrumpsTrigger1").WithCronSchedule("0 0 8 1/1 * ? *").ForJob(checkEbayForTopTrumps).Build();
            var checkEbayForTopTrumpsTrigger12pm = TriggerBuilder.Create().WithIdentity("checkEbayForTopTrumpsTrigger2").WithCronSchedule("0 0 12 1/1 * ? *").ForJob(checkEbayForTopTrumps).Build();
            var checkEbayForTopTrumpsTrigger5pm = TriggerBuilder.Create().WithIdentity("checkEbayForTopTrumpsTrigger3").WithCronSchedule("0 0 17 1/1 * ? *").ForJob(checkEbayForTopTrumps).Build();
            var checkEbayForTopTrumpsTrigger9pm = TriggerBuilder.Create().WithIdentity("checkEbayForTopTrumpsTrigger4").WithCronSchedule("0 0 21 1/1 * ? *").ForJob(checkEbayForTopTrumps).Build();

            await _scheduler.ScheduleJob(syncSpreadsheet, syncSpreadsheetTrigger);
            await _scheduler.ScheduleJob(apiRefresh, apiRefreshTrigger);
            await _scheduler.ScheduleJob(checkEbayForTopTrumpsTrigger);
            await _scheduler.ScheduleJob(checkEbayForTopTrumpsTrigger12pm);
            await _scheduler.ScheduleJob(checkEbayForTopTrumpsTrigger5pm);
            await _scheduler.ScheduleJob(checkEbayForTopTrumpsTrigger9pm);
            Log.Information("[Job Scheduler] Job Scheduler Setup");
        }
    }

    internal class SyncSpreadsheetJob : IJob
    {
        public async Task Execute(IJobExecutionContext context)
        {
            await GoogleSheets.GetTopTrumpsAndAddToDb();
        }
    }

    internal class CheckEbayJob : IJob
    {
        public async Task Execute(IJobExecutionContext context)
        {
            await EbayData.CheckAndSendNewTopTrumpPacks();
        }
    }

    internal class RefreshTokenJob : IJob
    {
        public static readonly IList<string> scopes = new List<string>()
            {
                "https://api.ebay.com/oauth/api_scope"
            };

        private static OAuth2Api oAuth2Api = new OAuth2Api();
        public async Task Execute(IJobExecutionContext context)
        {
            try
            {
                String yamlFile = @"config.yaml";
                StreamReader streamReader = new StreamReader(yamlFile);
                CredentialUtil.Load(streamReader);

                var refresh = oAuth2Api.GetAccessToken(OAuthEnvironment.PRODUCTION, Config.RefreshToken, scopes);

                var config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                config.AppSettings.Settings["EbayOAuth"].Value = refresh.AccessToken.Token;
                config.Save(ConfigurationSaveMode.Modified);
                ConfigurationManager.RefreshSection("appSettings");
                Config.EbayOAuth = refresh.AccessToken.Token;

                await Discord.InformOfEbayTokenRefresh(true);
                Log.Information($"[Refresh Job] Token {refresh.AccessToken.Token} successfully refreshed! Expires in: {Config.RefreshToken} | Refresh: ");
            }
            catch (Exception e)
            {
                Log.Fatal($"[Refresh Job] Failed to refresh token - {e}");
                await Discord.InformOfEbayTokenRefresh(false, e);
                return;
            }
        }
    }
}

