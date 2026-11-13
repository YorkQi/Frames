using System;

namespace Frame.Core.Uow
{
    /// <summary>
    /// 工作单元特性（Attribute）。
    /// 用于标记需要自动包裹工作单元的方法或类。
    /// 建议在 ApplicationService 中使用 <see cref="Application.ApplicationService"/> 基类，
    /// 其内置了自动 UOW 管理。
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
    public class UnitOfWorkAttribute : Attribute
    {
        /// <summary>
        /// 是否自动回滚（默认 false）。
        /// <c>true</c> 表示方法结束后无论是否调用 CompleteAsync 都自动回滚变更，
        /// 适用于只读查询场景。
        /// </summary>
        public bool IsAutoRollback { get; set; } = false;

        /// <summary>
        /// 工作单元超时时间（秒）。超过指定时间未提交则自动回滚。
        /// null 或 0 表示不限制。
        /// </summary>
        public int TimeoutSeconds { get; set; } = 0;

        /// <summary>获取 Timeout 的 TimeSpan 表示</summary>
        public TimeSpan? Timeout => TimeoutSeconds > 0 ? TimeSpan.FromSeconds(TimeoutSeconds) : null;
    }
}
