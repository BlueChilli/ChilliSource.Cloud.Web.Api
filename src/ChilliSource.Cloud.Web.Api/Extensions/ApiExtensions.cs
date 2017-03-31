using ChilliSource.Cloud.Core;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Controllers;
using System.Web.Http.Results;

namespace ChilliSource.Cloud.Web.Api
{
    public static class ApiExtensions
    {
        public static IHttpActionResult CreateApiErrorResponse(this HttpRequestMessage request, System.Web.Http.ModelBinding.ModelStateDictionary modelStateDictionary)
        {
            var errors = new List<string>();

            foreach (var modelState in modelStateDictionary.Values)
            {
                foreach (var error in modelState.Errors)
                {
                    if (error.Exception != null)
                    {
                        errors.Add(error.Exception.Message);
                    }
                    else
                    {
                        errors.Add(error.ErrorMessage);
                    }
                }
            }

            return new ResponseMessageResult(CreateApiErrorResponseMessage(request, errors));
        }

        public static IHttpActionResult CreateApiErrorResponse<T>(this HttpRequestMessage request, ServiceResult<T> result, HttpStatusCode statusCode = HttpStatusCode.BadRequest)
        {
            return new ResponseMessageResult(CreateApiErrorResponseMessage(request, result, statusCode));
        }

        public static IHttpActionResult CreateApiErrorResponseMessage(this HttpRequestMessage request, params string[] errors)
        {
            return new ResponseMessageResult(CreateApiErrorResponseMessage(request, (IEnumerable<string>)errors));
        }

        public static HttpResponseMessage CreateApiErrorResponseMessage(this HttpRequestMessage request, IEnumerable<string> errors, HttpStatusCode statusCode = HttpStatusCode.BadRequest)
        {
            var errorList = new List<string>();
            if (errors == null || errors.Count() == 0)
                errorList.Add("Oops, something went wrong. Please try again!");
            else
                errorList.AddRange(errors);

            return request.CreateResponse(statusCode, new ErrorResult { Errors = errorList });
        }

        public static HttpResponseMessage CreateApiErrorResponseMessage<T>(this HttpRequestMessage request, ServiceResult<T> result, HttpStatusCode statusCode = HttpStatusCode.BadRequest)
        {
            var errors = new List<string> { result.Error };

            return request.CreateResponse(statusCode, new ErrorResult { Errors = errors });
        }
    }
}
