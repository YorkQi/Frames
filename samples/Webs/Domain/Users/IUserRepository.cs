using System.Collections.Generic;
using System.Threading.Tasks;
using Frame.Core.Repositories;

namespace Domain.Users
{
    public interface IUserRepository :
        IRepository<int, User>
    {
        /// <summary>Dapper 原始 SQL 查询</summary>
        Task<IEnumerable<User>> QueryAsync();

        /// <summary>通过 GetDbConnection() 使用 Dapper 扩展</summary>
        Task<IEnumerable<User>> QueryByDapperAsync(string name);
    }
}
