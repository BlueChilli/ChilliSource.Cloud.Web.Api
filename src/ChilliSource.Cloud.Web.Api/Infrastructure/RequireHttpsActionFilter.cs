using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;

namespace ChilliSource.Cloud.Web.Api
{
    /// <summary>
    /// Forces request to be over https protocol. Works with AWS load balancing
    /// </summary>
    public class RequireHttpsActionFilter : ActionFilterAttribute
    {
        private const string Https = "https";
        private const string ForwardProto = "X-Forwarded-Proto";

        public override void OnActionExecuting(HttpActionContext actionContext)
        {
            var request = actionContext.Request;
            if (request.RequestUri.Scheme != Uri.UriSchemeHttps && !IsForwardedSsl(request.Headers) && !NotRequireHttps.IsTrue(request.Properties))
            {
                actionContext.Response = new HttpResponseMessage(HttpStatusCode.Forbidden)
                {
                    ReasonPhrase = "HTTPS Required"
                };
            }
            else
            {
                base.OnActionExecuting(actionContext);
            }
        }

        private static bool IsForwardedSsl(HttpRequestHeaders header)
        {
            var xForwardedProto = header.FirstOrDefault(x => x.Key == ForwardProto);
            var forwardedSsl = xForwardedProto.Value != null &&
                xForwardedProto.Value.Any(x => string.Equals(x, Https, StringComparison.InvariantCultureIgnoreCase));
            return forwardedSsl;
        }

    }

    /// <summary>
    /// By default all Api controllers and actions are https (RequireHttpsMessageHandler) use this attribute to allow http traffic to a controller or action. For example accessing ELB endpoints via ipaddress.
    /// </summary>
    public class NotRequireHttps : AuthorizationFilterAttribute
    {
        internal static bool IsTrue(IDictionary<string, object> properties)
        {
            if (properties != null && properties.ContainsKey(ApiConstant.NotRequireHttps))
            {
                return (bool)properties[ApiConstant.NotRequireHttps];
            }
            return false;
        }

        public override void OnAuthorization(HttpActionContext actionContext)
        {
            if (!actionContext.ControllerContext.Request.Properties.ContainsKey(ApiConstant.NotRequireHttps))
                actionContext.ControllerContext.Request.Properties.Add(ApiConstant.NotRequireHttps, true);

            base.OnAuthorization(actionContext);
        }
    }
}