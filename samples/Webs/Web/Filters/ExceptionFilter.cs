using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Web.Exceptions;

namespace Web.Filters
{
    public class ExceptionFilter(ILogger<ExceptionFilter> logger) : IExceptionFilter
    {

        public void OnException(ExceptionContext filterContext)
        {
            var exception = GetException<BusinessException>(filterContext.Exception);
            if (exception != null)
            {
                filterContext.Result = new JsonResult(
                    new ExceptionResult()
                    {
                        Code = exception?.Code ?? default,
                        Message = exception?.Message ?? string.Empty
                    })
                {
                    StatusCode = exception?.StateCode ?? StatusCodes.Status400BadRequest
                };
                logger.LogError(exception?.Message ?? string.Empty, exception);
            }
        }

        private TException? GetException<TException>(Exception? exception)
            where TException : Exception
        {
            if (exception == null)
            {
                return null;
            }
            if (exception is TException tException)
            {
                return tException;
            }
            else
            {
                return GetException<TException>(exception?.InnerException);
            }
        }
    }
}
