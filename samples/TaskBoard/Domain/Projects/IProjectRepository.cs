using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Frame.Core.Entities.Dtos;
using Frame.Core.Repositories;

namespace Domain.Projects
{
    /// <summary>项目仓储；实现类由框架按 IRepository 派生接口自动注册为 Scoped。</summary>
    public interface IProjectRepository : IRepository<int, Project>
    {
        /// <summary>按编码或名称模糊分页查询（只含未删除项目）</summary>
        Task<PageResult<Project>> QueryPagedAsync(string? keyword, int page, int size, CancellationToken cancellationToken = default);

        /// <summary>编码是否已被占用（未删除范围内）</summary>
        Task<bool> ExistsCodeAsync(string code, CancellationToken cancellationToken = default);

        /// <summary>立即保存挂起的变更（需要数据库生成主键时使用）</summary>
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
