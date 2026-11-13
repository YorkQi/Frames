using System;

namespace Application.Exceptions
{
    /// <summary>
    /// 业务异常：表示可以返回给调用方的业务错误。
    /// <see cref="Code"/> 是业务码，<see cref="StateCode"/> 是建议的 HTTP 状态码。
    /// </summary>
    public class BusinessException : Exception
    {
        public int? Code { get; set; }

        public int? StateCode { get; set; }

        public BusinessException()
        {
            StateCode = 400;
        }

        public BusinessException(string? message) : base(message)
        {
            StateCode = 400;
        }

        public BusinessException(int? code, string? message) : base(message)
        {
            Code = code;
            StateCode = 400;
        }

        public BusinessException(int? code, int? stateCode, string? message) : base(message)
        {
            Code = code;
            StateCode = stateCode;
        }

        public BusinessException(string? message, Exception? exception) : base(message, exception)
        {
            StateCode = 400;
        }

        public BusinessException(int? code, string? message, Exception? exception) : base(message, exception)
        {
            Code = code;
            StateCode = 400;
        }

        public BusinessException(int? code, int? stateCode, string? message, Exception? exception) : base(message, exception)
        {
            Code = code;
            StateCode = stateCode;
        }
    }
}
