using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Frame.Core.Uow
{
    /// <summary>
    /// 工作单元（Unit of Work）接口。
    /// 用于将多个数据库操作合并到同一个事务范围内，在 <see cref="CompleteAsync"/> 时一次性提交。
    /// </summary>
    public interface IUnitOfWork : IDisposable, IAsyncDisposable
    {
        /// <summary>工作单元唯一标识（Guid 的 N 格式，32 位十六进制字符串）</summary>
        string Id { get; }

        /// <summary>
        /// 工作单元内共享的键值容器。
        /// 框架自身用它挂接当前请求的数据库上下文（键为 <c>Frame.Databases.IDbContext</c>），
        /// 业务代码也可以用它传递请求级资源，避免额外的 AsyncLocal。
        /// </summary>
        IDictionary<string, object?> Items { get; }

        /// <summary>
        /// 提交当前工作单元的所有未持久化变更。
        /// </summary>
        /// <param name="cancellationToken">取消令牌</param>
        Task CompleteAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// 显式回滚当前工作单元的所有变更。
        /// </summary>
        Task RollbackAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// 注册提交处理器。最外层 UOW 提交时按注册顺序依次执行，
        /// 数据库层用它完成「开启事务 → SaveChanges → 提交 → 分发领域事件」。
        /// 注册到嵌套 UOW 上的处理器会被提升到根 UOW。
        /// </summary>
        /// <param name="handler">提交处理器</param>
        void RegisterCompletionHandler(Func<CancellationToken, Task> handler);

        /// <summary>
        /// 注册回滚处理器。最外层 UOW 回滚时按注册的逆序执行。
        /// </summary>
        /// <param name="handler">回滚处理器</param>
        void RegisterRollbackHandler(Action handler);

        /// <summary>
        /// 是否已完成（CompleteAsync 已成功调用）
        /// </summary>
        bool IsCompleted { get; }

        /// <summary>
        /// 是否已回滚（RollbackAsync 已调用）
        /// </summary>
        bool IsRolledBack { get; }
    }
}
