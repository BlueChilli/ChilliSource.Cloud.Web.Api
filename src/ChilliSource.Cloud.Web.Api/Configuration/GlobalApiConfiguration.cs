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

        private ApiSection _apiSection;

        public ApiSection GetApiSection(bool throwIfNotSet = true)
        {
            if (throwIfNotSet && _apiSection == null)
                throw new ApplicationException("ApiConfiguration is not set.");

            return _apiSection;
        }

        public GlobalApiConfiguration SetApiSection(ApiSection apiSection)
        {
            _apiSection = apiSection;
            return this;
        }
    }
}
