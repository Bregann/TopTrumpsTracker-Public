using Serilog;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EbayBot
{
    public class Database
    {
        private static string _sqlConnectionString = "Data Source=Database/TopTrumps.sqlite;Version=3;PRAGMA journal_mode=WAL";

        public static async Task ExecuteQuery(string query)
        {
            using (var sqlConnection = new SQLiteConnection(_sqlConnectionString))
            using (var sqlCommand = new SQLiteCommand(query, sqlConnection))
            {
                await sqlConnection.OpenAsync().ConfigureAwait(false);
                await sqlCommand.ExecuteNonQueryAsync().ConfigureAwait(false);
            }
        }

        public static async Task InsertTopTrumps(IList<IList<object>> topTrumps)
        {
            using (var connection = new SQLiteConnection(_sqlConnectionString))
            {
                await connection.OpenAsync();

                using (var transaction = connection.BeginTransaction())
                {
                    foreach (var topTrumpPack in topTrumps)
                    {
                        if (topTrumpPack[0].ToString() == "Name") //too lazy to figure out how to select from 2nd row down on sheets lol
                        {
                            continue;
                        }

                        //to add them
                        using (var sqlCommand = new SQLiteCommand($"INSERT OR IGNORE INTO topTrumps (packName, packOwned, timesFound) VALUES ('{topTrumpPack[0].ToString().Replace("'","")}', '{topTrumpPack[2]}', 0)", connection, transaction))
                        {
                            await sqlCommand.ExecuteNonQueryAsync();
                        }

                        //to make sure they're set to the correct owned status
                        using (var sqlCommand = new SQLiteCommand($"UPDATE topTrumps SET packOwned = '{topTrumpPack[2]}' WHERE packName = '{topTrumpPack[0].ToString().Replace("'", "")}'", connection, transaction))
                        {
                            await sqlCommand.ExecuteNonQueryAsync();
                        }
                    }

                    await transaction.CommitAsync();
                    await transaction.DisposeAsync();
                    await connection.CloseAsync();
                }
            }
        }

        public static async Task<List<string>> GetUnownedTopTrumps()
        {
            using (var sqlConnection = new SQLiteConnection(_sqlConnectionString))
            using (var sqlCommand = new SQLiteCommand("SELECT * FROM topTrumps where packOwned='No'", sqlConnection))
            {
                await sqlConnection.OpenAsync();
                var packs = new List<string>();

                using (var reader = await sqlCommand.ExecuteReaderAsync().ConfigureAwait(false))
                {
                    while (await reader.ReadAsync().ConfigureAwait(false))
                    {
                        packs.Add(reader["packName"].ToString());
                    }
                }

                return packs;
            }
        }

        public static async Task<bool> IsListingInDatabase(string ebayId)
        {
            using (var sqlConnection = new SQLiteConnection(_sqlConnectionString))
            using (var sqlCommand = new SQLiteCommand($"SELECT * FROM ebayListings WHERE ebayItemId=@id", sqlConnection))
            {
                sqlCommand.Parameters.Add("@id", DbType.String).Value = ebayId;

                await sqlConnection.OpenAsync();
                await sqlCommand.ExecuteNonQueryAsync();

                var result = await sqlCommand.ExecuteScalarAsync();

                if (result == DBNull.Value || result == null)
                {
                    return false;
                }

                return true;
            }
        }

        public static async Task InsertListingToDatabase(string packName, string ebayItemId, string price, string url, string buyingOptions, ulong discordId)
        {
            using (var sqlConnection = new SQLiteConnection(_sqlConnectionString))
            using (var sqlCommand = new SQLiteCommand($"INSERT INTO ebayListings (ebayItemId, price, url, buyingOption, discordMessageID, packName) VALUES (@itemid, @price, @url, @buyopt, @msgid, @packname)", sqlConnection))
            {
                sqlCommand.Parameters.Add("@itemid", DbType.String).Value = ebayItemId;
                sqlCommand.Parameters.Add("@msgid", DbType.UInt64).Value = discordId;
                sqlCommand.Parameters.Add("@packname", DbType.String).Value = packName;
                sqlCommand.Parameters.Add("@url", DbType.String).Value = url;
                sqlCommand.Parameters.Add("@buyopt", DbType.String).Value = buyingOptions;
                sqlCommand.Parameters.Add("@price", DbType.String).Value = price;

                await sqlConnection.OpenAsync().ConfigureAwait(false);
                await sqlCommand.ExecuteNonQueryAsync().ConfigureAwait(false);
            }

            await ExecuteQuery($"UPDATE topTrumps SET timesFound = timesFound + 1 WHERE packName = '{packName}'");
        }
    }
}
