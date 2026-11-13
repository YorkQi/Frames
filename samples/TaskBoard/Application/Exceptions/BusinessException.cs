using System;

namespace Application.Exceptions
{
    /// <summary>
    /// 业务异常：消息可以直接返回给调用方；<see cref="Code"/> 是业务码，
    /// <see cref="StateCode"/> 是建议的 HTTP 状态码（默认 400）。
    /// </summary>
    public class BusinessException : Exception
    {
        /// <summary>业务码</summary>
        public int? Code { get; set; }

        /// <summary>建议的 HTTP 状态码</summary>
        public int? StateCode { get; set; }

        /// <summary>创建业务异常（默认 400）</summary>
        public BusinessException(string? message) : base(message)
        {
            StateCode = 400;
        }

        /// <summary>创建带业务码的业务异常（默认 400）</summary>
        public BusinessException(int? code, string? message) : base(message)
        {
            Code = code;
            StateCode = 400;
        }

        /// <summary>创建带业务码与状态码的业务异常</summary>
        public BusinessException(int? code, int? stateCode, string? message) : base(message)
        {
            Code = code;
            StateCode = stateCode;
        }
    }
}
