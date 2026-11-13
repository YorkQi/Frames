using System;

namespace Frame.Core.Auditing
{
    /// <summary>
    /// 时间提供者。审计字段一律通过该接口取时间，便于测试中替换。
    /// </summary>
    public interface IClock
    {
        /// <summary>当前 UTC 时间。</summary>
        DateTime Now { get; }
    }

    /// <summary>
    /// 默认时间提供者，返回 <see cref="DateTime.UtcNow"/>。
    /// </summary>
    public sealed class SystemClock : IClock
    {
        /// <inheritdoc />
        public DateTime Now => DateTime.UtcNow;
    }
}
