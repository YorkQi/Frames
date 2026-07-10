using System.Collections.Generic;
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
        /// <summary>
        /// 基础 SQL 查询（无参数）
        /// </summary>
        public Task<IEnumerable<User>> QueryAsync()
        {
            var sql = "SELECT * FROM `Users` WHERE Id > 1 ; ";
            return DBContext.QueryAsync<User>(sql);
        }

        /// <summary>
        /// DbParameters 动态条件查询：根据传入参数动态拼接 SQL 条件。
        /// 用 AddIf 按需追加，无需写多个 if-else 拼匿名对象。
        /// </summary>
        public async Task<IEnumerable<User>> QueryByConditionAsync(string? name, UserSex? sex, int? status)
        {
            var sql = "SELECT * FROM `Users` WHERE 1=1";
            var p = new DbParameters()
                .AddIf(!string.IsNullOrEmpty(name), "Name", $"%{name}%")
                .AddIf(sex.HasValue, "Sex", sex!.Value)
                .AddIf(status.HasValue, "Status", status!.Value);

            if (!string.IsNullOrEmpty(name))
                sql += " AND Name LIKE @Name";
            if (sex.HasValue)
                sql += " AND Sex = @Sex";
            if (status.HasValue)
                sql += " AND Status = @Status";

            return await DBContext.QueryAsync<User>(sql, p);
        }

        /// <summary>
        /// QueryPaged 分页查询：一次往返完成 COUNT + 数据。
        /// 返回 PageResult&lt;User&gt; 包含当前页数据、总记录数。
        /// </summary>
        public Task<PageResult<User>> QueryPagedAsync(string? name, int page, int size)
        {
            var sql = "SELECT * FROM `Users` WHERE 1=1";
            var p = new DbParameters();

            if (!string.IsNullOrEmpty(name))
            {
                sql += " AND Name LIKE @Name";
                p.Add("Name", $"%{name}%");
            }

            return DBContext.QueryPagedAsync<User>(sql, p, page, size);
        }
    }
}
