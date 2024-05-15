using Frame.Core.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Frame.Core.Filters
{
    public class ModelValidattionAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            //验证使用验证的
            if (!context.ModelState.IsValid && context.ModelState.ErrorCount > 0)
            {
                //如果模型验证有错误的话 则返回错误信息
                var errors = new List<ExceptionResult>();
                foreach (var modelState in context.ModelState)
                {
                    foreach (var item in modelState.Value.Errors)
                    {
                        errors.Add(new ExceptionResult()
                        {
                            Code = (int)FrameExceptionCode.Invalidate,
                            Message = $"{modelState.Key}:{item.ErrorMessage}"
                        });
                    }
                }
                context.Result = new JsonResult(errors)
                {
                    StatusCode = StatusCodes.Status400BadRequest
                };
            }
        }

        public override void OnActionExecuted(ActionExecutedContext context)
        {
            //Action操作之后不做操作
        }
    }
}
