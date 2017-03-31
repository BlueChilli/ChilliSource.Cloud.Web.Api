using ChilliSource.Cloud.Core;
using System;
using System.Threading.Tasks;
using System.Web.Http.ExceptionHandling;

namespace ChilliSource.Cloud.Web.Api
{
    public class UnhandledExceptionHandler : ExceptionHandler
    {
        Func<ExceptionHandlerContext, bool> _shouldExpose = (e) => false;

        public UnhandledExceptionHandler ExposeExceptionWhen(Func<ExceptionHandlerContext, bool> shouldExpose)
        {
            _shouldExpose = shouldExpose ?? ((e) => false);
            return this;
        }

        public override void Handle(ExceptionHandlerContext context)
        {
            base.Handle(context);

            var result = new ServiceResult
            {
                Success = false,
                Error = "Oops, something went wrong. Please try again!"
            };

            if (_shouldExpose(context))
            {
                result.Error = context.Exception.ToString();
            }

            context.Result = context.Request.CreateApiErrorResponse(result);
        }

        public async override Task HandleAsync(ExceptionHandlerContext context, System.Threading.CancellationToken cancellationToken)
        {
            await base.HandleAsync(context, cancellationToken);

            var result = new ServiceResult
            {
                Success = false,
                Error = "Oops, something went wrong. Please try again!"
            };

            if (_shouldExpose(context))
            {
                result.Error = context.Exception.ToString();
            }

            context.Result = context.Request.CreateApiErrorResponse(result, System.Net.HttpStatusCode.InternalServerError);
        }

        public override bool ShouldHandle(ExceptionHandlerContext context)
        {
            return true;
        }
    }
}