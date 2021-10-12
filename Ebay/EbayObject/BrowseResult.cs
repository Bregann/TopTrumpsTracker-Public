using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EbayBot.Ebay.EbayObject
{
    public class BrowseResult
    {
        [JsonProperty("itemSummaries")]
        public List<ItemSummary> ItemSummaries { get; set; }
    }
}
