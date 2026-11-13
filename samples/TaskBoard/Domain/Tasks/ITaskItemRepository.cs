using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Frame.Core.Repositories;

namespace Domain.Tasks
{
    /// <summary>任务仓储。</summary>
    public interface ITaskItemRepository : IRepository<int, TaskItem>
    {
        /// <summary>查询某项目下的任务（只含未删除）</summary>
        Task<IEnumerable<TaskItem>> QueryByProjectAsync(int projectId, CancellationToken cancellationToken = default);

        /// <summary>统计某项目下未完成的任务数</summary>
        Task<int> CountOpenAsync(int projectId, CancellationToken cancellationToken = default);

        /// <summary>查询已逾期且未完成的任务（供后台扫描任务使用）</summary>
        Task<IEnumerable<TaskItem>> GetOverdueAsync(DateTime utcNow, CancellationToken cancellationToken = default);

        /// <summary>立即保存挂起的变更</summary>
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
