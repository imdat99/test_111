using Acm.Api.Helpers;
using HotChocolate;
using HotChocolate.AspNetCore.Serialization;
using HotChocolate.Execution;
using System.Net;

namespace Acm.Api.Extensions
{
    /// <summary>
    /// Custom for GraphQL error.
    /// Spec errors: https://spec.graphql.org/June2018/#example-fce18
    /// </summary>
    public class CustomErrorFilterExtensions : IErrorFilter
    {
        public IError OnError(IError error)
        {
            var message = error.Message;
            var errorCode = error.Code;
            if (error.Exception != null)
            {
                var httpStatusCode = error.Exception.InnerException != null ?
                        error.Exception.InnerException.GetStatusCode() :
                        error.Exception.GetStatusCode();
                if (httpStatusCode != null)
                    errorCode = (int)httpStatusCode + "";   //keep is "number" of HttpStatusCode

                message = error.Exception.InnerException != null ?
                    error.Exception.InnerException.Message :
                    error.Exception.Message;
            }

            return Utilities.BuildError(message, errorCode, error.Exception, error.Path, error.Extensions, error.Locations);
        }
    }

    /// <summary>
    /// Điều chỉnh lại định dạng HttpStatusCode trả về cho hợp lý với exception (mặc định luôn là 200 hoặc 500 không hợp lý!)
    /// </summary>
    public class CustomHttpResponseFormatter : DefaultHttpResponseFormatter
    {
        protected override HttpStatusCode OnDetermineStatusCode(IOperationResult result, FormatInfo format, HttpStatusCode? proposedStatusCode)
        {
            if (result.Errors?.Count > 0)
            {
                var error = result.Errors[0];
                var ex = error.Exception;
                if (ex != null)
                {
                    var statusCode = ex.InnerException != null ?
                        ex.InnerException.GetStatusCode() :
                        ex.GetStatusCode();

                    if (statusCode != null)
                        return statusCode.Value;
                }
            }

            // In all other cases let Hot Chocolate figure out the
            // appropriate status code.
            return base.OnDetermineStatusCode(result, format, proposedStatusCode);
        }
    }
}
