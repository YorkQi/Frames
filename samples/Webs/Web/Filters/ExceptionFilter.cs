using Application.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Web.Exceptions;

namespace Web.Filters
{
    /// <summary>
    /// 统一异常出口：业务异常返回其业务码与建议状态码，
    /// 资源不存在返回 404，其余异常交给框架默认处理（500）。
    /// </summary>
    public sealed class ExceptionFilter(ILogger<ExceptionFilter> logger) : IExceptionFilter
    {
        /// <inheritdoc />
        public void OnException(ExceptionContext context)
        {
            var businessException = Find<BusinessException>(context.Exception);
            if (businessException is not null)
            {
                context.Result = new JsonResult(new ExceptionResult
                {
                    Code = businessException.Code,
                    Message = businessException.Message
                })
                {
                    StatusCode = businessException.StateCode ?? StatusCodes.Status400BadRequest
                };

                context.ExceptionHandled = true;
                logger.LogWarning(businessException, "业务异常：{Message}", businessException.Message);
                return;
            }

            var notFoundException = Find<KeyNotFoundException>(context.Exception);
            if (notFoundException is not null)
            {
                context.Result = new JsonResult(new ExceptionResult
                {
                    Message = notFoundException.Message
                })
                {
                    StatusCode = StatusCodes.Status404NotFound
                };

                context.ExceptionHandled = true;
                logger.LogWarning("资源不存在：{Message}", notFoundException.Message);
            }
        }

        private static TException? Find<TException>(Exception? exception) where TException : Exception
        {
            if (exception is null)
                return null;

            if (exception is TException match)
                return match;

            return Find<TException>(exception.InnerException);
        }
    }
}
