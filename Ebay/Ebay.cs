using DSharpPlus.Entities;
using EbayBot.Ebay.EbayObject;
using Newtonsoft.Json;
using RestSharp;
using Serilog;
using System;
using System.Threading.Tasks;
namespace EbayBot
{
    public class EbayData
    {
        public static async Task<BrowseResult> PerformTopTrumpSearch(string packName)
        {
            try
            {
                //Create the request
                var client = new RestClient("https://api.ebay.com/buy/browse/v1/item_summary/");
                var request = new RestRequest($"search?q=\"{packName} top trumps\"&filter=buyingOptions:" + "{FIXED_PRICE|BEST_OFFER|AUCTION}&filter=price:[..3],priceCurrency:GBP&sort=price", Method.GET);
                request.AddHeader("X-EBAY-C-MARKETPLACE-ID", "EBAY_GB");
                request.AddHeader("Authorization", $"Bearer {Config.EbayOAuth}");

                //Get the response and Deserialize
                var response = await client.ExecuteTaskAsync(request);
                var responseDeserialized = JsonConvert.DeserializeObject<BrowseResult>(response.Content);
                return responseDeserialized;
            }
            catch (Exception e)
            {
                Log.Fatal($"[Top Trump Search] Failed to perform Top Trump Ebay search - {e}");
                return null;
            }
        }

        public static async Task CheckAndSendNewTopTrumpPacks()
        {
            Log.Information("[Top Trumps Check Job] Pack check started");

            //Get the unowned packs from the database
            var unownedPacks = await Database.GetUnownedTopTrumps();
            unownedPacks.Add("");
            unownedPacks.Add("rare ");

            //Its either a bug or I've got them all lol
            if (unownedPacks.Count == 0)
            {
                Log.Information("[Top Trumps Check Job] No unowned packs seen");
                return;
            }

            Log.Information($"[Top Trumps Check Job] {unownedPacks.Count} unowned packs to check");

            //Setup variables for stats
            var totalListingsFound = 0;
            var totalPacksWithNoListings = 0;
            var totalOverPricedListings = 0;
            var totalAlreadyNotifiedListings = 0;
            var totalListingsNotified = 0;
            var ebayErrors = 0;
            var processingErrors = 0;

            foreach (var pack in unownedPacks)
            {
                BrowseResult ebayPackData;

                try
                {
                    //perform the search
                    ebayPackData = await PerformTopTrumpSearch(pack);
                }
                catch (Exception e)
                {
                    Log.Fatal($"[Top Trumps Check Job] Error getting Top Trump data - {e}");
                    ebayErrors++;
                    continue;
                }

                //See if anything was found if not then null
                if (ebayPackData.ItemSummaries == null)
                {
                    Log.Information($"[Top Trumps Check Job] no packs found for {pack}");
                    totalPacksWithNoListings++;
                    continue;
                }

                Log.Information($"[Top Trumps Check Job] {ebayPackData.ItemSummaries.Count} packs found for {pack}");
                totalListingsFound += ebayPackData.ItemSummaries.Count;

                //Go through each listing
                foreach (var listing in ebayPackData.ItemSummaries)
                {
                    try
                    {
                        //encode url to begin with
                        var encodedUrl = System.Web.HttpUtility.UrlEncode(listing.ItemWebUrl);

                        //probably collection in person so just return
                        if (listing.ShippingOptions == null)
                        {
                            await Database.InsertListingToDatabase(pack, listing.ItemId, "dunno", System.Web.HttpUtility.UrlEncode(listing.ItemWebUrl), "dunno", 0);
                            Log.Information($"[Top Trumps Check Job] Listing {listing.ItemId} no shipping options - did not notify");
                            continue;
                        }

                        //Build the embed
                        var discordEmbed = new DiscordEmbedBuilder()
                        {
                            Title = $"{listing.Title}",
                            Timestamp = DateTime.Now,
                            Color = new DiscordColor(245, 39, 39),
                            Url = listing.ItemWebUrl
                        };

                        //Image can somwhow sometimes be null
                        if (listing.Image != null)
                        {
                            discordEmbed.WithImageUrl(listing.Image.ImageUrl);
                        }

                        //Add in some fields
                        discordEmbed.WithFooter($"Top Trump Tracker");
                        if (pack == "")
                        {
                            discordEmbed.AddField("Pack name", "general search");
                        }
                        else
                        {
                            discordEmbed.AddField("Pack name", pack);
                        }

                        //Check if its already in the daterbase
                        if (await Database.IsListingInDatabase(listing.ItemId))
                        {
                            //Don't need it so we continue
                            Log.Information($"[Top Trumps Check Job] Listing {listing.ItemId} already been sent");
                            totalAlreadyNotifiedListings++;
                            continue;
                        }

                        //Put together buy types
                        string buyTypes = "";
                        string price;

                        foreach (var type in listing.BuyingOptions)
                        {
                            buyTypes = buyTypes + type + " | ";
                        }



                        //auctions have different prices
                        if (listing.BuyingOptions.Contains("AUCTION"))
                        {
                            var totalPrice = (listing.CurrentBidPrice.Value + listing.ShippingOptions[0].ShippingCost.Value);
                            price = totalPrice.ToString("0.##");

                            if (totalPrice > 3.50)
                            {
                                totalOverPricedListings++;
                                Log.Information($"[Top Trumps Check Job] Auction Listing {listing.ItemId} price+postage too high - did not notify");
                                continue;
                            }

                            discordEmbed.AddField("Price", "£" + price);
                        }
                        else
                        {
                            var totalPrice = (listing.Price.Value + listing.ShippingOptions[0].ShippingCost.Value);
                            price = totalPrice.ToString("0.##");

                            if (totalPrice > 3.50)
                            {
                                totalOverPricedListings++;
                                Log.Information($"[Top Trumps Check Job] BIN Listing {listing.ItemId} price+postage too high - did not notify");
                                continue;
                            }

                            discordEmbed.AddField("Price", "£" + price);
                        }

                        //Add the buy types
                        discordEmbed.AddField("Buying options", buyTypes);

                        //Send it and add to database
                        var msgId = await Discord.SendFoundEbayListing(discordEmbed);

                        //if its 0 then it failed to send embed
                        if (msgId == 0)
                        {
                            continue;
                        }

                        //Add to daterbase
                        await Database.InsertListingToDatabase(pack, listing.ItemId, price, encodedUrl, buyTypes, msgId);
                        totalListingsNotified++;
                        Log.Information($"[Top Trumps Check Job] Listing {listing.ItemId} added to database");
                        await Task.Delay(1000);
                    }
                    catch (Exception e)
                    {
                        Log.Fatal($"[Top Trumps Check Job] Error processing - {e}");
                        Log.Fatal($"[Top Trumps Check Job] Debug info - {listing.ItemWebUrl}");
                        processingErrors++;
                        continue;
                    }
                }
            }

            await Discord.SendResultsEmbed(unownedPacks.Count, totalListingsFound, totalOverPricedListings, totalAlreadyNotifiedListings, totalListingsNotified, totalPacksWithNoListings, ebayErrors, processingErrors);
            Log.Information("[Top Trumps Check Job] Done");
        }
    }
}
