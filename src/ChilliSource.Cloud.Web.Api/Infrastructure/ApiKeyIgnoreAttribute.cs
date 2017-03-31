using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;

namespace ChilliSource.Cloud.Web.Api
{
    public class ApiKeyIgnore : AuthorizationFilterAttribute
    {
        public override void OnAuthorization(HttpActionContext actionContext)
        {
            SetCheckApiKeyFlag(actionContext);
        }

        private void SetCheckApiKeyFlag(HttpActionContext actionContext)
        {
            if (!actionContext.Request.Properties.ContainsKey(ApiConstant.ShouldCheckApiKey))
            {
                actionContext.ControllerContext.Request.Properties.Add(ApiConstant.ShouldCheckApiKey, false);
            }
        }
    }
}
