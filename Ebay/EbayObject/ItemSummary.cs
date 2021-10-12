using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EbayBot.Ebay.EbayObject
{
    public class ItemSummary
    {
        [JsonProperty("itemId")]
        public string ItemId { get; set; }

        [JsonProperty("itemWebUrl")]
        public string ItemWebUrl { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("image")]
        public Image Image { get; set; }

        [JsonProperty("shippingOptions")]
        public List<ShippingOption> ShippingOptions { get; set; }

        [JsonProperty("buyingOptions")]
        public List<string> BuyingOptions { get; set; }

        [JsonProperty("currentBidPrice", NullValueHandling = NullValueHandling.Ignore)]
        public Price CurrentBidPrice { get; set; }

        [JsonProperty("price", NullValueHandling = NullValueHandling.Ignore)]
        public Price Price { get; set; }
    }
}
