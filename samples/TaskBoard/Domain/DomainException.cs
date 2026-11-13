using System;

namespace Domain
{
    /// <summary>领域规则被违反时抛出（与"请求参数非法"区分）。</summary>
    public class DomainException(string message) : Exception(message);
}
