using System;
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
        /// 提交当前工作单元的所有未持久化变更。
        /// </summary>
        /// <param name="cancellationToken">取消令牌</param>
        Task CompleteAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// 显式回滚当前工作单元的所有变更。
        /// </summary>
        Task RollbackAsync(CancellationToken cancellationToken = default);

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
