using System.Threading;
using System.Threading.Tasks;
using Domain.Projects;
using Frame.Core.Entities.Dtos;
using Frame.Databases;
using Frame.Databases.Repositories;

namespace Infrastructure.Repositories
{
    /// <summary>项目仓储：Dapper 负责读，EF Core 通道负责写（写操作延迟到工作单元提交）。</summary>
    public class ProjectRepository : Repository<int, Project>, IProjectRepository
    {
        /// <inheritdoc />
        public Task<PageResult<Project>> QueryPagedAsync(string? keyword, int page, int size, CancellationToken cancellationToken = default)
        {
            var sql = "SELECT * FROM `Projects` WHERE IsDeleted = 0";
            var parameters = new DbParameters();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                sql += " AND (Code LIKE @Keyword OR Name LIKE @Keyword)";
                parameters.Add("Keyword", $"%{keyword.Trim()}%");
            }

            sql += " ORDER BY Id DESC";
            return DBContext.QueryPagedAsync<Project>(sql, parameters, page, size, cancellationToken: cancellationToken);
        }

        /// <inheritdoc />
        public async Task<bool> ExistsCodeAsync(string code, CancellationToken cancellationToken = default)
        {
            const string sql = "SELECT COUNT(*) FROM `Projects` WHERE Code = @Code AND IsDeleted = 0";
            var count = await DBContext.ExecuteScalarAsync<long>(
                sql,
                new DbParameters().Add("Code", code.Trim().ToUpperInvariant()),
                cancellationToken: cancellationToken);

            return count > 0;
        }

        /// <inheritdoc />
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
            => DBContext.SaveChangesAsync(cancellationToken);
    }
}
