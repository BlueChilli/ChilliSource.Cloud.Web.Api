using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;
using ChilliSource.Cloud.Core;

namespace ChilliSource.Cloud.Web.Api
{
    public class ApiKeyValidationAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(HttpActionContext actionContext)
        {
            var apiConfig = ApiSection.GetConfig();

            if (ShouldCheckApiKey(actionContext))
            {
                const string apiKeyHeaderKey = "ApiKey";
                var apiKey = HttpContext.Current.Request.Headers[apiKeyHeaderKey];
                apiKey = apiKey ?? HttpContext.Current.Request.Headers[apiKeyHeaderKey.ToLower()];
                apiKey = apiKey ?? HttpContext.Current.Request.QueryString[apiKeyHeaderKey];

                if (string.Compare(apiConfig.ApiKey, apiKey, StringComparison.InvariantCultureIgnoreCase) != 0)
                {
                    var result = ServiceResult.AsError("Api key is invalid");
                    actionContext.Response = actionContext.Request.CreateApiErrorResponseMessage(result);
                }
            }

            base.OnActionExecuting(actionContext);
        }

        public static bool ShouldCheckApiKey(HttpActionContext actionContext)
        {
            if (actionContext.Request != null && actionContext.Request.Properties != null && actionContext.Request.Properties.ContainsKey(ApiConstant.ShouldCheckApiKey))
            {
                return (bool)actionContext.Request.Properties[ApiConstant.ShouldCheckApiKey];
            }

            return true;
        }
    }
}