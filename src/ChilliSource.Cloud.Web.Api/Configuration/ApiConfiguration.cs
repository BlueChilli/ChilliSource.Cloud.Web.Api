using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChilliSource.Cloud.Web.Api
{
    public class ApiSection : ConfigurationSection
    {
        public static ApiSection GetConfig()
        {
            return GlobalApiConfiguration.Instance.GetApiSection();
        }

        [ConfigurationProperty("apikey")]
        public string ApiKey
        {
            get
            {
                return (string)this["apikey"];
            }
            set
            {
                this["apikey"] = value;
            }
        }
    }
}
