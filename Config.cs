using Serilog;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EbayBot
{
    public class Config
    {
        public static string EbayOAuth { get; set; }
        public static string RefreshToken { get; set; }
        public static void LoadConfig()
        {
            var config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            EbayOAuth = config.AppSettings.Settings["EbayOAuth"].Value;
            RefreshToken = config.AppSettings.Settings["RefreshToken"].Value;
            Log.Information("[Config] Config loaded");
        }
    }
}
