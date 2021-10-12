using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EbayBot.Ebay.EbayObject
{
    public class Price
    {
        [JsonProperty("value")]
        public double Value { get; set; }
    }
}
