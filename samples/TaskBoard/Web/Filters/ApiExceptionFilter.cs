using Application.Exceptions;
using Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Web.Exceptions;

namespace Web.Filters
{
    /// <summary>
    /// 统一异常出口：
    /// 业务异常 → 业务码 + 建议状态码；领域规则冲突 → 400；资源不存在 → 404；
    /// 其余异常不处理，交给框架返回 500。
    /// </summary>
    public sealed class ApiExceptionFilter(ILogger<ApiExceptionFilter> logger) : IExceptionFilter
    {
        /// <inheritdoc />
        public void OnException(ExceptionContext context)
        {
            var businessException = Find<BusinessException>(context.Exception);
            if (businessException is not null)
            {
                Write(context, businessException.StateCode ?? StatusCodes.Status400BadRequest,
                    new ApiError(businessException.Code, businessException.Message));
                logger.LogWarning("业务异常：{Message}", businessException.Message);
                return;
            }

            var domainException = Find<DomainException>(context.Exception);
            if (domainException is not null)
            {
                Write(context, StatusCodes.Status400BadRequest, new ApiError(2001, domainException.Message));
                logger.LogWarning("领域规则冲突：{Message}", domainException.Message);
                return;
            }

            var notFoundException = Find<KeyNotFoundException>(context.Exception);
            if (notFoundException is not null)
            {
                Write(context, StatusCodes.Status404NotFound, new ApiError(2002, notFoundException.Message));
                logger.LogWarning("资源不存在：{Message}", notFoundException.Message);
            }
        }

        private static void Write(ExceptionContext context, int statusCode, ApiError error)
        {
            context.Result = new JsonResult(error) { StatusCode = statusCode };
            context.ExceptionHandled = true;
        }

        private static TException? Find<TException>(Exception? exception) where TException : Exception
        {
            if (exception is null)
                return null;

            return exception as TException ?? Find<TException>(exception.InnerException);
        }
    }
}
