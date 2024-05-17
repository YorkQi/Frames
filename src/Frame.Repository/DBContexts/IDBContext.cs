using Frame.Core.Entitys;
using Frame.Core.Entitys.Dtos;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace Frame.Repository.DBContexts
{
    public interface IDBContext
    { 
        void Initialize(string connectionString);
        Task InitializeAsync(string connectionString);


        void BeginTransaction(IsolationLevel level = IsolationLevel.Unspecified);
        void Commit();
        void Rollback();

        #region 基础操作
        Task<TEntity> Get<TEntity>(object id);
        Task<IEnumerable<TEntity>> QueryAllEntity<TEntity>();
        Task<IEnumerable<TEntity>> QueryEntity<TEntity>(object ids);
        Task<int> Insert<TEntity>(TEntity entity) where TEntity : IEntity;
        Task<int> InsertBatch<TEntity>(IEnumerable<TEntity> entitys) where TEntity : IEntity;
        Task<int> Update<TEntity>(TEntity entity) where TEntity : IEntity;
        Task<int> UpdateBatch<TEntity>(IEnumerable<TEntity> entitys) where TEntity : IEntity;
        Task<int> Delete<TEntity>(object id) where TEntity : IEntity;
        Task<int> DeleteBatch<TEntity>(object ids) where TEntity : IEntity;
        #endregion

        #region SQL处理
        int Execute(string sql, object? param = null, int? commandTimeout = null, CommandType? commandType = null);
        Task<int> ExecuteAsync(string sql, object? param = null, int? commandTimeout = null, CommandType? commandType = null);
        IDataReader ExecuteReader(string sql, object? param = null, int? commandTimeout = null, CommandType? commandType = null);
        Task<IDataReader> ExecuteReaderAsync(string sql, object? param = null, int? commandTimeout = null, CommandType? commandType = null);
        IDataReader ExecuteReader(string sql, CommandBehavior commandBehavior, object? param = null, int? commandTimeout = null, CommandType? commandType = null);
        Task<IDataReader> ExecuteReaderAsync(string sql, CommandBehavior commandBehavior, object? param = null, int? commandTimeout = null, CommandType? commandType = null);
        TResult ExecuteScalar<TResult>(string sql, object? param = null, int? commandTimeout = null, CommandType? commandType = null);
        Task<TResult> ExecuteScalarAsync<TResult>(string sql, object? param = null, int? commandTimeout = null, CommandType? commandType = null);
        IEnumerable<TResult> Query<TResult>(string sql, object? param = null, int? commandTimeout = null, CommandType? commandType = null);
        Task<IEnumerable<TResult>> QueryAsync<TResult>(string sql, object? param = null, int? commandTimeout = null, CommandType? commandType = null);
        TResult QueryFirst<TResult>(string sql, object? param = null, int? commandTimeout = null, CommandType? commandType = null);
        Task<TResult> QueryFirstAsync<TResult>(string sql, object? param = null, int? commandTimeout = null, CommandType? commandType = null);
        TResult QueryFirstOrDefault<TResult>(string sql, object? param = null, int? commandTimeout = null, CommandType? commandType = null);
        Task<TResult> QueryFirstOrDefaultAsync<TResult>(string sql, object? param = null, int? commandTimeout = null, CommandType? commandType = null);
        TResult QuerySingle<TResult>(string sql, object? param = null, int? commandTimeout = null, CommandType? commandType = null);
        Task<TResult> QuerySingleAsync<TResult>(string sql, object? param = null, int? commandTimeout = null, CommandType? commandType = null);
        TResult QuerySingleOrDefault<TResult>(string sql, object? param = null, int? commandTimeout = null, CommandType? commandType = null);
        Task<TResult> QuerySingleOrDefaultAsync<TResult>(string sql, object? param = null, int? commandTimeout = null, CommandType? commandType = null);
        Task<PageResult<TResult>> QueryPageAsync<TResult>(string sql, object? param = null, int? page = 1, int? limit = 20, int? commandTimeout = null, CommandType? commandType = null);
        #endregion
    }
}
