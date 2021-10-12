using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Util.Store;
using Serilog;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace EbayBot
{

    internal class GoogleSheets
    {
        private static string[] Scopes = { SheetsService.Scope.Spreadsheets };
        private static SheetsService _googleSheetsService;
        private static string _spreadsheetId = "1ruQknmTxPwnQLuM2cCZTwS-Hnd9DK2u2Pwe2dzbPwEI";

        public static async Task GoogleSheetsSetup()
        {
            UserCredential credential;
            //TODO: Find out how to make the default format -> number to text
            //Load credentials that are from Google
            var stream = new FileStream("client_id.json", FileMode.Open, FileAccess.Read);

            // The file token.json stores the user's access and refresh tokens, and is created
            // automatically when the authorization flow completes for the first time.
            //Thanks Google

            var credPath = "token.json";

            credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(GoogleClientSecrets.Load(stream).Secrets,
                Scopes,
                "user",
                CancellationToken.None,
                new FileDataStore(credPath, true));

            _googleSheetsService = new SheetsService(new BaseClientService.Initializer
            {
                HttpClientInitializer = credential,
                ApplicationName = "TopTrumps"
            });
        }

        public static async Task GetTopTrumpsAndAddToDb()
        {
            try
            {
                var range = "TopTrumps!B:D";
                var request = _googleSheetsService.Spreadsheets.Values.Get(_spreadsheetId, range);

                var response = await request.ExecuteAsync();
                var values = response.Values;

                await Database.InsertTopTrumps(values);
                await Discord.InformOfGoogleSheetsSync(true);
                Log.Information($"[Daily Sync Job] Packs updated");
            }
            catch (Exception e)
            {
                await Discord.InformOfGoogleSheetsSync(false, e);
                Log.Fatal($"[Daily Sync Job] Failed to update - {e}");
            }
        }
    }
}
