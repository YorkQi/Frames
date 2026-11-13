using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Domain.Users;
using Domain.Users.Enums;
using Frame.Core.Entities.Dtos;
using Frame.Databases;
using Frame.Databases.Repositories;

namespace Repository
{
    public class UserRepository :
        Repository<int, User>,
        IUserRepository
    {
        /// <summary>基础 SQL 查询（无参数），只取未删除的数据</summary>
        public Task<IEnumerable<User>> QueryAsync(CancellationToken cancellationToken = default)
        {
            var sql = "SELECT * FROM `Users` WHERE IsDeleted = 0 AND Id > 1;";
            return DBContext.QueryAsync<User>(sql, cancellationToken: cancellationToken);
        }

        /// <summary>
        /// DbParameters 动态条件查询：根据传入参数动态拼接 SQL 条件，
        /// 参数始终以绑定变量形式传给 Dapper，避免拼接注入。
        /// </summary>
        public Task<IEnumerable<User>> QueryByConditionAsync(string? name, UserSex? sex, CancellationToken cancellationToken = default)
        {
            var sql = "SELECT * FROM `Users` WHERE IsDeleted = 0";
            var p = new DbParameters();

            if (!string.IsNullOrWhiteSpace(name))
            {
                sql += " AND Name LIKE @Name";
                p.Add("Name", $"%{name.Trim()}%");
            }

            if (sex.HasValue)
            {
                sql += " AND Sex = @Sex";
                p.Add("Sex", sex.Value);
            }

            return DBContext.QueryAsync<User>(sql, p, cancellationToken: cancellationToken);
        }

        /// <summary>分页查询：一次往返完成 COUNT + 数据。</summary>
        public Task<PageResult<User>> QueryPagedAsync(string? name, int page, int size, CancellationToken cancellationToken = default)
        {
            var sql = "SELECT * FROM `Users` WHERE IsDeleted = 0";
            var p = new DbParameters();

            if (!string.IsNullOrWhiteSpace(name))
            {
                sql += " AND Name LIKE @Name";
                p.Add("Name", $"%{name.Trim()}%");
            }

            sql += " ORDER BY Id DESC";

            return DBContext.QueryPagedAsync<User>(sql, p, page, size, cancellationToken: cancellationToken);
        }

        /// <summary>名称是否已被占用。</summary>
        public async Task<bool> ExistsNameAsync(string name, CancellationToken cancellationToken = default)
        {
            const string sql = "SELECT COUNT(*) FROM `Users` WHERE Name = @Name AND IsDeleted = 0";
            var count = await DBContext.ExecuteScalarAsync<long>(
                sql,
                new DbParameters().Add("Name", name.Trim()),
                cancellationToken: cancellationToken);

            return count > 0;
        }

        /// <summary>立即保存变更（需要数据库生成主键时使用）。</summary>
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
            => DBContext.SaveChangesAsync(cancellationToken);
    }
}
