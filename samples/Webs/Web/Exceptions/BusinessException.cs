namespace Web.Exceptions
{
    /// <summary>
    /// 业务异常基类，用于表示业务逻辑层面的错误。
    /// 注意：避免与 System.Net.WebException 混淆，故命名为 BusinessException。
    /// </summary>
    public class BusinessException : Exception
    {
        public int? Code { get; set; }
        public int? StateCode { get; set; }

        public BusinessException()
            : base()
        {
            StateCode = 400;
        }

        public BusinessException(string? message)
            : base(message)
        {
            StateCode = 400;
        }

        public BusinessException(int? code, string? message)
            : base(message)
        {
            Code = code;
            StateCode = 400;
        }

        public BusinessException(int? code, int? statusCode, string? message)
            : base(message)
        {
            Code = code;
            StateCode = statusCode;
        }

        public BusinessException(string? message, Exception? exception)
            : base(message, exception)
        {
            StateCode = 400;
        }

        public BusinessException(int? code, string? message, Exception? exception)
           : base(message, exception)
        {
            Code = code;
            StateCode = 400;
        }

        public BusinessException(int? code, int? stateCode, string? message, Exception? exception)
           : base(message, exception)
        {
            Code = code;
            StateCode = stateCode;
        }
    }
}

