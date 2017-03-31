using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;

namespace ChilliSource.Cloud.Web.Api
{
    public class EnableProgressAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuted(HttpActionExecutedContext actionExecutedContext)
        {
            if (actionExecutedContext.Response != null && actionExecutedContext.Response.Headers != null)
            {
                actionExecutedContext.Response.Headers.TransferEncodingChunked = false;
            }

            base.OnActionExecuted(actionExecutedContext);
        }

        public override Task OnActionExecutedAsync(HttpActionExecutedContext actionExecutedContext, CancellationToken cancellationToken)
        {
            if (actionExecutedContext.Response != null && actionExecutedContext.Response.Headers != null)
            {
                actionExecutedContext.Response.Headers.TransferEncodingChunked = false;
            }

            return base.OnActionExecutedAsync(actionExecutedContext, cancellationToken);
        }
    }
}