using Microsoft.AspNetCore.Http;
using System;

namespace Frame.Core.Exceptions
{
    public class WebException : Exception
    {
        public int? Code { get; set; }
        public int? StateCode { get; set; }

        public WebException()
            : base()
        {
            StateCode = StatusCodes.Status400BadRequest;
        }

        public WebException(string? message)
            : base(message)
        {
            StateCode = StatusCodes.Status400BadRequest;
        }

        public WebException(int? code, string? message)
            : base(message)
        {
            Code = code;
            StateCode = StatusCodes.Status400BadRequest;
        }

        public WebException(int? code, int? statusCode, string? message)
            : base(message)
        {
            Code = code;
            StateCode = statusCode;
        }

        public WebException(string? message, Exception? exception)
            : base(message, exception)
        {
            StateCode = StatusCodes.Status400BadRequest;
        }

        public WebException(int? code, string? message, Exception? exception)
           : base(message, exception)
        {
            Code = code;
            StateCode = StatusCodes.Status400BadRequest;
        }

        public WebException(int? code, int? stateCode, string? message, Exception? exception)
           : base(message, exception)
        {
            Code = code;
            StateCode = stateCode;
        }
    }
}
