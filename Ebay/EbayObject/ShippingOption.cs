using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EbayBot.Ebay.EbayObject
{
    public class ShippingOption
    {
        [JsonProperty("shippingCost")]
        public Price ShippingCost { get; set; }
    }
}
