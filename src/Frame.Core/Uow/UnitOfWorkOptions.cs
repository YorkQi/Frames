using System;

namespace Frame.Core.Uow
{
    /// <summary>
    /// 工作单元配置选项。
    /// 在调用 <c>IUnitOfWorkManager.Begin(options)</c> 时传入。
    /// </summary>
    public class UnitOfWorkOptions
    {
        /// <summary>
        /// 是否自动回滚（默认 false）。
        /// <c>true</c>：无论是否调用 <see cref="IUnitOfWork.CompleteAsync"/>，
        /// 在 Dispose 时自动回滚所有变更。
        /// 适用于只读查询或测试场景。
        /// </summary>
        public bool IsAutoRollback { get; set; } = false;

        /// <summary>
        /// 工作单元超时时间。超过指定时间未提交则自动回滚。
        /// null 表示不限制超时。
        /// </summary>
        public TimeSpan? Timeout { get; set; } = null;
    }
}
