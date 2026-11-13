using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Domain.Tasks;
using Frame.Databases;
using Frame.Databases.Repositories;

namespace Infrastructure.Repositories
{
    /// <summary>任务仓储：全部走 Dapper 读，写走上层 Repository 的 EF Core 通道。</summary>
    public class TaskItemRepository : Repository<int, TaskItem>, ITaskItemRepository
    {
        /// <inheritdoc />
        public Task<IEnumerable<TaskItem>> QueryByProjectAsync(int projectId, CancellationToken cancellationToken = default)
        {
            const string sql = "SELECT * FROM `TaskItems` WHERE ProjectId = @ProjectId AND IsDeleted = 0 ORDER BY Id DESC";
            return DBContext.QueryAsync<TaskItem>(
                sql,
                new DbParameters().Add("ProjectId", projectId),
                cancellationToken: cancellationToken);
        }

        /// <inheritdoc />
        public async Task<int> CountOpenAsync(int projectId, CancellationToken cancellationToken = default)
        {
            const string sql = "SELECT COUNT(*) FROM `TaskItems` WHERE ProjectId = @ProjectId AND Status <> @Done AND IsDeleted = 0";
            var count = await DBContext.ExecuteScalarAsync<long>(
                sql,
                new DbParameters()
                    .Add("ProjectId", projectId)
                    .Add("Done", (int)TaskItemStatus.Done),
                cancellationToken: cancellationToken);

            return (int)count;
        }

        /// <inheritdoc />
        public Task<IEnumerable<TaskItem>> GetOverdueAsync(DateTime utcNow, CancellationToken cancellationToken = default)
        {
            const string sql = "SELECT * FROM `TaskItems` WHERE DueDate IS NOT NULL AND DueDate < @Now AND Status <> @Done AND IsDeleted = 0";
            return DBContext.QueryAsync<TaskItem>(
                sql,
                new DbParameters()
                    .Add("Now", utcNow)
                    .Add("Done", (int)TaskItemStatus.Done),
                cancellationToken: cancellationToken);
        }

        /// <inheritdoc />
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
            => DBContext.SaveChangesAsync(cancellationToken);
    }
}
