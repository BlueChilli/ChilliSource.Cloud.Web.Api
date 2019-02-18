using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChilliSource.Cloud.Web.Api
{
    public class GlobalApiConfiguration
    {
        private static readonly GlobalApiConfiguration _instance = new GlobalApiConfiguration();
        public static GlobalApiConfiguration Instance { get { return _instance; } }

        private GlobalApiConfiguration() { }

        string _apiKey = null;
        public string ApiKey
        {
            get
            {
                if (String.IsNullOrEmpty(_apiKey))
                    throw new ApplicationException("Api key not set.");

                return _apiKey;
            }
            set { _apiKey = value; }
        }
    }
}
