using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Microsoft.Extensions.Logging;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;

namespace EbayBot
{
    internal class Discord
    {
        public static DiscordClient DiscordClient;
        private static ulong _topTrumpsChannel = 852675053702217748;
        public static async Task MainAsync()
        {
            //Setup logging
            var logFactory = new LoggerFactory().AddSerilog();

            DiscordClient = new DiscordClient(new DiscordConfiguration()
            {
                Token = "Token",
                TokenType = TokenType.Bot
            });

            //Connect
            await DiscordClient.ConnectAsync();
            DiscordClient.Ready += Ready;
            DiscordClient.MessageReactionAdded += MessageReactionAdded;
        }

        private static async Task MessageReactionAdded(DiscordClient sender, MessageReactionAddEventArgs e)
        {
            if (e.Channel.Id != _topTrumpsChannel || e.User.Id != 196695995868774400)
            {
                return;
            }

            try
            {
               await e.Message.DeleteAsync();
            }
            catch (Exception ex)
            {
                Log.Fatal($"[Discord Reaction] Error deleting message - {ex}");
                return;
            }
        }

        private static async Task Ready(DiscordClient sender, ReadyEventArgs e)
        {
            Log.Information("[Discord Connection] Discord Client Ready");
            var discordActivity = new DiscordActivity($"👀 eBay", ActivityType.Watching);
            await DiscordClient.UpdateStatusAsync(discordActivity);
        }

        public static async Task<ulong> SendFoundEbayListing(DiscordEmbedBuilder embed)
        {
            try
            {
                var channel = await DiscordClient.GetChannelAsync(_topTrumpsChannel);
                await channel.TriggerTypingAsync();

                var message = await channel.SendMessageAsync(embed);
                await message.CreateReactionAsync(DiscordEmoji.FromUnicode("👍"));

                return message.Id;
            }
            catch (Exception e)
            {
                Log.Fatal($"[Send Discord Embed] Failed to send Embed - {e}");
                return 0;
            }
        }

        public static async Task SendResultsEmbed(int numberOfPacksSearched, int totalListingsFound, int totalOverPricedListings, int totalAlreadyNotifiedListings, int totalListingsNotified, int totalPacksWithNoListings, int ebayErrors, int processingErrors)
        {
            //Build the embed
            var discordEmbed = new DiscordEmbedBuilder()
            {
                Title = $"Top Tumps Search Result",
                Timestamp = DateTime.Now,
                Color = new DiscordColor(43, 235, 252)
            };

            discordEmbed.AddField("Number of Packs Searched", numberOfPacksSearched.ToString("N0"), true);
            discordEmbed.AddField("Total Listings Found", totalListingsFound.ToString("N0"), true);
            discordEmbed.AddField("Total Overpriced Listings", totalOverPricedListings.ToString("N0"), true);
            discordEmbed.AddField("Total Already Notified Listings", totalAlreadyNotifiedListings.ToString("N0"), true);
            discordEmbed.AddField("Total Listings Notifed", totalListingsNotified.ToString("N0"), true);
            discordEmbed.AddField("Total Packs With No listings", totalPacksWithNoListings.ToString("N0"), true);
            discordEmbed.AddField("Ebay Errors", ebayErrors.ToString("N0"), true);
            discordEmbed.AddField("Processing Errors", processingErrors.ToString("N0"), true);

            var channel = await DiscordClient.GetChannelAsync(_topTrumpsChannel);
            await channel.TriggerTypingAsync();

            await channel.SendMessageAsync(discordEmbed);
        }

        public static async Task InformOfEbayTokenRefresh(bool sucessful, Exception e = null)
        {
            var channel = await DiscordClient.GetChannelAsync(_topTrumpsChannel);
            await channel.TriggerTypingAsync();

            if (sucessful)
            {
                //Build the embed
                var discordEmbed = new DiscordEmbedBuilder()
                {
                    Title = $"Ebay Refresh Successful",
                    Timestamp = DateTime.Now,
                    Color = new DiscordColor(43, 235, 252)
                };

                await channel.SendMessageAsync(discordEmbed);
            }
            else
            {
                var discordEmbed = new DiscordEmbedBuilder()
                {
                    Title = $"Ebay Refresh NOT SUCCESFUL",
                    Timestamp = DateTime.Now,
                    Color = new DiscordColor(255, 70, 33)
                };

                discordEmbed.WithDescription(e.ToString());
                await channel.SendMessageAsync(discordEmbed);
            }
        }

        public static async Task InformOfGoogleSheetsSync(bool sucessful, Exception e = null)
        {
            var channel = await DiscordClient.GetChannelAsync(_topTrumpsChannel);
            await channel.TriggerTypingAsync();

            if (sucessful)
            {
                //Build the embed
                var discordEmbed = new DiscordEmbedBuilder()
                {
                    Title = $"Google Sheets Sync Successful",
                    Timestamp = DateTime.Now,
                    Color = new DiscordColor(43, 235, 252)
                };

                await channel.SendMessageAsync(discordEmbed);
            }
            else
            {
                var discordEmbed = new DiscordEmbedBuilder()
                {
                    Title = $"Google Sheets Sync NOT SUCCESFUL",
                    Timestamp = DateTime.Now,
                    Color = new DiscordColor(255, 70, 33)
                };

                discordEmbed.WithDescription(e.ToString());
                await channel.SendMessageAsync(discordEmbed);
            }
        }
    }
}
