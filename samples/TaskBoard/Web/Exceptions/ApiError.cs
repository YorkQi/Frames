namespace Web.Exceptions
{
    /// <summary>统一错误响应体。</summary>
    public sealed record ApiError(int? Code, string? Message);
}
