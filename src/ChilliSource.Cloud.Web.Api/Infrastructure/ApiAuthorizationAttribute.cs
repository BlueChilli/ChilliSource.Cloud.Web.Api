using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;
using System.Net;
using System.Security.Principal;
using System.Web.Http;
using ChilliSource.Cloud.Core;

namespace ChilliSource.Cloud.Web.Api
{ 
    public class ApiAuthorizationAttribute : AuthorizationFilterAttribute
    {
        public string[] Roles { get; set; }

        public override void OnAuthorization(HttpActionContext actionContext)
        {
            if (!AllowsAnonymous(actionContext))
            {
                if (HttpContext.Current == null || HttpContext.Current.User == null || !HttpContext.Current.User.Identity.IsAuthenticated)
                {
                    var requestUrl = HttpContext.Current == null ? "" : HttpContext.Current.Request.Url.PathAndQuery;
                    var result = ServiceResult.AsError("Authentication failed. Request Url -> " + requestUrl);
                    actionContext.Response = actionContext.Request.CreateApiErrorResponseMessage(result, HttpStatusCode.Unauthorized);
                }
                else if (!HasAuthorizedRole(HttpContext.Current.User))
                {
                    var requestUrl = HttpContext.Current == null ? "" : HttpContext.Current.Request.Url.PathAndQuery;
                    var result = ServiceResult.AsError("You are not authorized to view this page. Request Url -> " + requestUrl);
                    actionContext.Response = actionContext.Request.CreateApiErrorResponseMessage(result, HttpStatusCode.Forbidden);
                }
            }

            base.OnAuthorization(actionContext);
        }

        private bool AllowsAnonymous(HttpActionContext actionContext)
        {
            return actionContext.ActionDescriptor.GetCustomAttributes<AllowAnonymousAttribute>(false).Any() ||
                   actionContext.ActionDescriptor.ControllerDescriptor.GetCustomAttributes<AllowAnonymousAttribute>(true).Any();
        }

        private bool HasAuthorizedRole(IPrincipal user)
        {
            if (this.Roles == null)
                return true;

            return this.Roles.Where(r => user.IsInRole(r)).Any();
        }
    }
}