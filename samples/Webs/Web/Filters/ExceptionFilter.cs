using Frame.Core.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Frame.Core.Filters
{
    public class ExceptionFilter : IExceptionFilter
    {
        private readonly ILogger _logger;
        public ExceptionFilter(ILogger<ExceptionFilter> logger)
        {
            _logger = logger;
        }

        public void OnException(ExceptionContext filterContext)
        {
            var exception = GetException<WebException>(filterContext.Exception);
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
                _logger.LogError(exception?.Message ?? string.Empty, exception);
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
