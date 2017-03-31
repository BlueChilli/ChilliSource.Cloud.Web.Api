using ChilliSource.Cloud.Core;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Results;

namespace ChilliSource.Cloud.Web.Api
{
    public class BaseApiController : ApiController
    {
        [DebuggerNonUserCodeAttribute]
        public IApiServiceCallerSyntax<T> ApiServiceCall<T>(Func<ServiceResult<T>> action)
        {
            return (new ApiServiceCaller<T>(this)).SetAction(action);
        }

        [DebuggerNonUserCodeAttribute]
        public IApiServiceCallerSyntax<ApiPagedList<T>> ApiServiceCall<T>(Func<ApiPagedList<T>> action)
        {
            Func<ServiceResult<ApiPagedList<T>>> wrappedAction = () => ServiceResult<ApiPagedList<T>>.AsSuccess(action());

            return (new ApiServiceCaller<ApiPagedList<T>>(this)).SetAction(wrappedAction);
        }

        [DebuggerNonUserCodeAttribute]
        public IApiServiceCallerAsyncSyntax<T> ApiServiceCall<T>(Func<Task<ServiceResult<T>>> action)
        {
            return (new ApiServiceCallerAsync<T>(this)).SetAction(action);
        }

        [DebuggerNonUserCodeAttribute]
        public IApiServiceCallerAsyncSyntax<ApiPagedList<T>> ApiServiceCall<T>(Func<Task<ApiPagedList<T>>> action)
        {
            Func<Task<ServiceResult<ApiPagedList<T>>>> wrappedAction = async () =>
            {
                var result = await action();
                return ServiceResult<ApiPagedList<T>>.AsSuccess(result);
            };

            return (new ApiServiceCallerAsync<ApiPagedList<T>>(this)).SetAction(wrappedAction);
        }

        [DebuggerNonUserCodeAttribute]
        new internal OkNegotiatedContentResult<T> Ok<T>(T content)
        {
            return base.Ok(content);
        }

        [DebuggerNonUserCodeAttribute]
        new internal OkResult Ok()
        {
            return base.Ok();
        }
    }

    public interface IApiServiceCallerSyntax<T>
    {
        IApiServiceCallerSyntax<T> OnSuccess(Func<ServiceResult<T>, IHttpActionResult> onSuccess);
        IApiServiceCallerSyntax<T> OnFailure(Func<IHttpActionResult> onFailure);
        IApiServiceCallerSyntax<T> OnFailure(Func<ServiceResult<T>, IHttpActionResult> onFailure);
        IHttpActionResult Call(bool modelIsRequired = true);
    }

    public interface IApiServiceCallerAsyncSyntax<T>
    {
        IApiServiceCallerAsyncSyntax<T> OnSuccess(Func<ServiceResult<T>, Task<IHttpActionResult>> onSuccess);
        IApiServiceCallerAsyncSyntax<T> OnFailure(Func<Task<IHttpActionResult>> onFailure);
        IApiServiceCallerAsyncSyntax<T> OnFailure(Func<ServiceResult<T>, Task<IHttpActionResult>> onFailure);

        IApiServiceCallerAsyncSyntax<T> OnSuccess(Func<ServiceResult<T>, IHttpActionResult> onSuccess);
        IApiServiceCallerAsyncSyntax<T> OnFailure(Func<IHttpActionResult> onFailure);
        IApiServiceCallerAsyncSyntax<T> OnFailure(Func<ServiceResult<T>, IHttpActionResult> onFailure);
        Task<IHttpActionResult> Call(bool modelIsRequired = true);
    }

    [DebuggerNonUserCodeAttribute]
    internal class ApiServiceCaller<T> : IApiServiceCallerSyntax<T>
    {
        Func<ServiceResult<T>> _action;
        Func<ServiceResult<T>, IHttpActionResult> _onSuccess;
        Func<ServiceResult<T>, IHttpActionResult> _onFailure;
        BaseApiController _controller;

        public ApiServiceCaller(BaseApiController controller)
        {
            _controller = controller;
            //default action for success;
            if (typeof(T) == typeof(object))
            {
                this.OnSuccess((response) => (response.Result == null ? (IHttpActionResult)_controller.Ok()
                                                                        : (IHttpActionResult)_controller.Ok(response.Result)));
            }
            else
            {
                this.OnSuccess((response) => _controller.Ok(response.Result));
            }

            //default action for failure;
            this.OnFailure((response) => _controller.Request.CreateApiErrorResponse(response, response.StatusCode));
        }

        [DebuggerNonUserCodeAttribute]
        public IApiServiceCallerSyntax<T> SetAction(Func<ServiceResult<T>> action)
        {
            _action = action; return this;
        }

        [DebuggerNonUserCodeAttribute]
        public IApiServiceCallerSyntax<T> OnSuccess(Func<ServiceResult<T>, IHttpActionResult> onSuccess)
        {
            _onSuccess = onSuccess; return this;
        }

        [DebuggerNonUserCodeAttribute]
        public IApiServiceCallerSyntax<T> OnFailure(Func<IHttpActionResult> onFailure)
        {
            _onFailure = (T) => onFailure(); return this;
        }

        [DebuggerNonUserCodeAttribute]
        public IApiServiceCallerSyntax<T> OnFailure(Func<ServiceResult<T>, IHttpActionResult> onFailure)
        {
            _onFailure = onFailure; return this;
        }

        [DebuggerNonUserCodeAttribute]
        public IHttpActionResult Call(bool modelIsRequired = true)
        {
            if (_action == null || _onSuccess == null || _onFailure == null)
            {
                throw new ApplicationException("You need to set up the service call.");
            }

            var arguments = _controller.ActionContext.ActionArguments;
            if (modelIsRequired && arguments.Count > 0)
            {
                var nullArguments = _controller.ActionContext.ActionArguments.Where(a => a.Value == null);
                foreach (var argument in nullArguments)
                {
                    _controller.ModelState.AddModelError(argument.Key, $"Argument {argument.Key} cannot be null");
                }
            }

            if (!_controller.ModelState.IsValid)
            {
                return _controller.Request.CreateApiErrorResponse(_controller.ModelState);
            }

            ServiceResult<T> response = _action();
            if (response.Success)
            {
                return _onSuccess(response);
            }

            return _onFailure(response);
        }
    }

    [DebuggerNonUserCodeAttribute]
    internal class ApiServiceCallerAsync<T> : IApiServiceCallerAsyncSyntax<T>
    {
        Func<Task<ServiceResult<T>>> _action;
        Func<ServiceResult<T>, Task<IHttpActionResult>> _onSuccess;
        Func<ServiceResult<T>, Task<IHttpActionResult>> _onFailure;
        BaseApiController _controller;

        public ApiServiceCallerAsync(BaseApiController controller)
        {
            _controller = controller;
            //default action for success;
            if (typeof(T) == typeof(object))
            {
                this.OnSuccess((response) => (response.Result == null ? (IHttpActionResult)_controller.Ok()
                                                                        : (IHttpActionResult)_controller.Ok(response.Result)));
            }
            else
            {
                this.OnSuccess((response) => _controller.Ok(response.Result));
            }

            //default action for failure;
            this.OnFailure((response) => _controller.Request.CreateApiErrorResponse(response, response.StatusCode));
        }

        [DebuggerNonUserCodeAttribute]
        public ApiServiceCallerAsync<T> SetAction(Func<Task<ServiceResult<T>>> action)
        {
            _action = action; return this;
        }

        [DebuggerNonUserCodeAttribute]
        public IApiServiceCallerAsyncSyntax<T> OnSuccess(Func<ServiceResult<T>, Task<IHttpActionResult>> onSuccess)
        {
            _onSuccess = onSuccess; return this;
        }

        [DebuggerNonUserCodeAttribute]
        public IApiServiceCallerAsyncSyntax<T> OnFailure(Func<Task<IHttpActionResult>> onFailure)
        {
            _onFailure = (T) => onFailure(); return this;
        }

        [DebuggerNonUserCodeAttribute]
        public IApiServiceCallerAsyncSyntax<T> OnFailure(Func<ServiceResult<T>, Task<IHttpActionResult>> onFailure)
        {
            _onFailure = onFailure; return this;
        }

        //Sync continuation
        [DebuggerNonUserCodeAttribute]
        public IApiServiceCallerAsyncSyntax<T> OnSuccess(Func<ServiceResult<T>, IHttpActionResult> onSuccess)
        {
            _onSuccess = (T) => Task.FromResult(onSuccess(T)); return this;
        }

        //Sync continuation
        [DebuggerNonUserCodeAttribute]
        public IApiServiceCallerAsyncSyntax<T> OnFailure(Func<IHttpActionResult> onFailure)
        {
            _onFailure = (T) => Task.FromResult(onFailure()); return this;
        }

        //Sync continuation
        [DebuggerNonUserCodeAttribute]
        public IApiServiceCallerAsyncSyntax<T> OnFailure(Func<ServiceResult<T>, IHttpActionResult> onFailure)
        {
            _onFailure = (T) => Task.FromResult(onFailure(T)); return this;
        }

        [DebuggerNonUserCodeAttribute]
        public async Task<IHttpActionResult> Call(bool modelIsRequired = true)
        {
            if (_action == null || _onSuccess == null || _onFailure == null)
            {
                throw new ApplicationException("You need to set up the service call.");
            }

            var arguments = _controller.ActionContext.ActionArguments;
            if (modelIsRequired && arguments.Count > 0)
            {
                var nullArguments = arguments.Where(a => a.Value == null);
                foreach (var argument in nullArguments)
                {
                    _controller.ModelState.AddModelError(argument.Key, $"Argument {argument.Key} cannot be null");
                }
            }

            if (!_controller.ModelState.IsValid)
            {
                return _controller.Request.CreateApiErrorResponse(_controller.ModelState);
            }

            ServiceResult<T> response = await _action();
            if (response.Success)
            {
                return await _onSuccess(response);
            }

            return await _onFailure(response);
        }
    }

    internal class ApiConstant
    {
        public static string ShouldCheckApiKey = "__ShouldCheckApiKey";

        public static string NotRequireHttps = "__NotRequireHttps";
    }

}
