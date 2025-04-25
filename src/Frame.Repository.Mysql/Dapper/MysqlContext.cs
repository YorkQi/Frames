using Dapper;
using Frame.Repository.DBContexts;
using Frame.Repository.Entitys;
using Frame.Repository.Entitys.Dtos;
using Frame.Repository.Mysql.ConnectionBuilder;
using Frame.Repository.Mysql.DataObjectModel;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using static Dapper.SqlMapper;

namespace Frame.Repository.Mysql
{
    public partial class MysqlContext : IDBContext
    {
        private IDbConnection? _dbConnection;
        private IDbTransaction? _dbTransaction;

        private readonly IDBConnectionBuilder _mysqlBuilder;
        public MysqlContext(IDBConnectionBuilder mysqlBuilder)
        {
            _mysqlBuilder = mysqlBuilder;

        }
        public void Initialize(string connectionString)
        {
            _dbConnection = _mysqlBuilder.GetDbConnection(connectionString);
        }

        public async Task InitializeAsync(string connectionString)
        {
            _dbConnection = await _mysqlBuilder.GetDbConnectionAsync(connectionString);
        }

        #region Transaction

        public void BeginTransaction(IsolationLevel level = IsolationLevel.Unspecified)
        {
            ConnectionOpen();
            if (_dbTransaction == null)
            {
                if (_dbConnection == null)
                {
                    throw new DataException("没有数据库连接对象");
                }
                _dbTransaction = _dbConnection?.BeginTransaction(level);
            }
        }

        public void Commit()
        {
            if (_dbConnection == null)
            {
                throw new DataException("没有数据库连接对象");
            }
            if (_dbConnection.State == ConnectionState.Open)
            {
                if (_dbTransaction != null)
                {
                    _dbTransaction.Commit();
                    ConnectionClose();
                    _dbTransaction.Dispose();
                }
            }
        }

        public void Rollback()
        {
            if (_dbConnection == null)
            {
                throw new DataException("没有数据库连接对象");
            }
            if (_dbConnection.State == ConnectionState.Open)
            {
                if (_dbTransaction != null)
                {
                    _dbTransaction.Rollback();
                    ConnectionClose();
                    _dbTransaction.Dispose();
                }
            }
        }

        private void ConnectionOpen()
        {
            if (_dbConnection == null)
            {
                throw new DataException("没有数据库连接对象");
            }
            if (_dbConnection.State != ConnectionState.Closed)
            {
                _dbConnection.Close();
            }
            _dbConnection.Open();
        }

        private void ConnectionClose()
        {
            if (_dbConnection == null)
            {
                throw new DataException("没有数据库连接对象");
            }
            if (_dbConnection.State != ConnectionState.Closed)
            {
                _dbConnection.Close();
            }
        }


        #endregion

        #region 基础操作

        public Task<TEntity> Get<TEntity>(object id)
        {
            var param = new DynamicParameters();
            param.Add("Id", id);
            string sql = $"SELECT * FROM {typeof(TEntity).Name} WHERE  Id=@Id;";
            return QueryFirstOrDefaultAsync<TEntity>(sql, param);
        }

        public Task<IEnumerable<TEntity>> QueryAllEntity<TEntity>()
        {
            string sql = $"SELECT * FROM {typeof(TEntity).Name};";
            return QueryAsync<TEntity>(sql);
        }

        public Task<IEnumerable<TEntity>> QueryEntity<TEntity>(object ids)
        {
            var param = new DynamicParameters();
            param.Add("Ids", ids);
            string sql = $"SELECT * FROM {typeof(TEntity).Name} WHERE  Id IN @Ids;";
            return QueryAsync<TEntity>(sql, param);
        }

        public Task<int> Insert<TEntity>(TEntity entity) where TEntity : IEntity
        {
            var dataTable = DataObjectModelHandler.ToTable(entity);
            if (dataTable is null) throw new ArgumentException(nameof(dataTable));
            var param = new DynamicParameters();
            foreach (var item in dataTable.Columns)
            {
                param.Add(item.Name, item.Value);
            }
            return QuerySingleOrDefaultAsync<int>(MysqlCommand.InsertToSql(dataTable), param);
        }

        public Task<int> InsertBatch<TEntity>(IEnumerable<TEntity> entitys) where TEntity : IEntity
        {
            var dataTables = DataObjectModelHandler.ToTable(entitys);
            if (!dataTables.Any()) throw new ArgumentException(nameof(dataTables));
            var param = new DynamicParameters();
            for (int i = 0; i < dataTables.Count(); i++)
            {
                foreach (var item in dataTables[i].Columns)
                {
                    param.Add($"{item.Name}_{i}", item.Value);
                }
            }
            return ExecuteAsync(MysqlCommand.InsertBatchToSql(dataTables), param);
        }

        public Task<int> Update<TEntity>(TEntity entity) where TEntity : IEntity
        {
            var dataTable = DataObjectModelHandler.ToTable(entity);
            var param = new DynamicParameters();
            foreach (var item in dataTable.Columns)
            {
                if (item.Value is not null)
                {
                    param.Add(item.Name, item.Value);
                }
            }
            param.Add(dataTable.KeyColumn.Name, dataTable.KeyColumn.Value);
            return ExecuteAsync(MysqlCommand.UpdateToSql(dataTable), param);
        }

        public Task<int> UpdateBatch<TEntity>(IEnumerable<TEntity> entitys) where TEntity : IEntity
        {
            var dataTables = DataObjectModelHandler.ToTable(entitys);
            var param = new DynamicParameters();
            for (int i = 0; i < dataTables.Count(); i++)
            {
                foreach (var item in dataTables[i].Columns)
                {
                    param.Add($"{item.Name}_{i}", item.Value);
                }
                param.Add($"{dataTables[i].KeyColumn.Name}_{i}", dataTables[i].KeyColumn.Value);
            }

            return ExecuteAsync(MysqlCommand.UpdateBatchToSql(dataTables), param);
        }

        public Task<int> Delete<TEntity>(object id) where TEntity : IEntity
        {
            var param = new DynamicParameters();
            param.Add("Id", id);
            string sql = $@" DELETE FROM {typeof(TEntity).Name} WHERE  Id=@Id;";
            return ExecuteAsync(sql, param);
        }

        public Task<int> DeleteBatch<TEntity>(object ids) where TEntity : IEntity
        {
            var param = new DynamicParameters();
            param.Add("Ids", ids);
            string sql = $@" DELETE FROM {typeof(TEntity).Name} WHERE  Id  IN @Ids;";
            return ExecuteAsync(sql, param);
        }

        #endregion

        #region Dapper

        public virtual int Execute(string sql, object? param = null, int? commandTimeout = null, CommandType? commandType = null)
        {
            return _dbConnection.Execute(sql: sql, param: param, transaction: _dbTransaction, commandTimeout: commandTimeout, commandType: commandType);
        }
        public virtual Task<int> ExecuteAsync(string sql, object? param = null, int? commandTimeout = null, CommandType? commandType = null)
        {
            return _dbConnection.ExecuteAsync(sql: sql, param: param, transaction: _dbTransaction, commandTimeout: commandTimeout, commandType: commandType);
        }
        public virtual IDataReader ExecuteReader(string sql, object? param = null, int? commandTimeout = null, CommandType? commandType = null)
        {
            return _dbConnection.ExecuteReader(sql: sql, param: param, transaction: _dbTransaction, commandTimeout: commandTimeout, commandType: commandType);
        }
        public virtual Task<IDataReader> ExecuteReaderAsync(string sql, object? param = null, int? commandTimeout = null, CommandType? commandType = null)
        {
            return _dbConnection.ExecuteReaderAsync(sql: sql, param: param, transaction: _dbTransaction, commandTimeout: commandTimeout, commandType: commandType);
        }
        public virtual IDataReader ExecuteReader(string sql, CommandBehavior commandBehavior, object? param = null, int? commandTimeout = null, CommandType? commandType = null)
        {
            return _dbConnection.ExecuteReader(sql: sql, param: param, transaction: _dbTransaction, commandTimeout: commandTimeout, commandType: commandType);
        }
        public virtual Task<IDataReader> ExecuteReaderAsync(string sql, CommandBehavior commandBehavior, object? param = null, int? commandTimeout = null, CommandType? commandType = null)
        {
            return _dbConnection.ExecuteReaderAsync(sql: sql, param: param, transaction: _dbTransaction, commandTimeout: commandTimeout, commandType: commandType);
        }
        public virtual TResult ExecuteScalar<TResult>(string sql, object? param = null, int? commandTimeout = null, CommandType? commandType = null)
        {
            return _dbConnection.ExecuteScalar<TResult>(sql: sql, param: param, transaction: _dbTransaction, commandTimeout: commandTimeout, commandType: commandType);
        }
        public virtual Task<TResult> ExecuteScalarAsync<TResult>(string sql, object? param = null, int? commandTimeout = null, CommandType? commandType = null)
        {
            return _dbConnection.ExecuteScalarAsync<TResult>(sql: sql, param: param, transaction: _dbTransaction, commandTimeout: commandTimeout, commandType: commandType);
        }
        public virtual IEnumerable<TResult> Query<TResult>(string sql, object? param = null, int? commandTimeout = null, CommandType? commandType = null)
        {
            return _dbConnection.Query<TResult>(sql: sql, param: param, transaction: _dbTransaction, commandTimeout: commandTimeout, commandType: commandType);
        }
        public virtual Task<IEnumerable<TResult>> QueryAsync<TResult>(string sql, object? param = null, int? commandTimeout = null, CommandType? commandType = null)
        {
            return _dbConnection.QueryAsync<TResult>(sql: sql, param: param, transaction: _dbTransaction, commandTimeout: commandTimeout, commandType: commandType);
        }
        public virtual TResult QueryFirst<TResult>(string sql, object? param = null, int? commandTimeout = null, CommandType? commandType = null)
        {
            return _dbConnection.QueryFirst<TResult>(sql: sql, param: param, transaction: _dbTransaction, commandTimeout: commandTimeout, commandType: commandType);
        }
        public virtual Task<TResult> QueryFirstAsync<TResult>(string sql, object? param = null, int? commandTimeout = null, CommandType? commandType = null)
        {
            return _dbConnection.QueryFirstAsync<TResult>(sql: sql, param: param, transaction: _dbTransaction, commandTimeout: commandTimeout, commandType: commandType);
        }
        public virtual TResult QueryFirstOrDefault<TResult>(string sql, object? param = null, int? commandTimeout = null, CommandType? commandType = null)
        {
            return _dbConnection.QueryFirstOrDefault<TResult>(sql: sql, param: param, transaction: _dbTransaction, commandTimeout: commandTimeout, commandType: commandType);
        }
        public virtual Task<TResult> QueryFirstOrDefaultAsync<TResult>(string sql, object? param = null, int? commandTimeout = null, CommandType? commandType = null)
        {
            return _dbConnection.QueryFirstOrDefaultAsync<TResult>(sql: sql, param: param, transaction: _dbTransaction, commandTimeout: commandTimeout, commandType: commandType);
        }
        public virtual TResult QuerySingle<TResult>(string sql, object? param = null, int? commandTimeout = null, CommandType? commandType = null)
        {
            return _dbConnection.QuerySingle<TResult>(sql: sql, param: param, transaction: _dbTransaction, commandTimeout: commandTimeout, commandType: commandType);
        }
        public virtual Task<TResult> QuerySingleAsync<TResult>(string sql, object? param = null, int? commandTimeout = null, CommandType? commandType = null)
        {
            return _dbConnection.QuerySingleAsync<TResult>(sql: sql, param: param, transaction: _dbTransaction, commandTimeout: commandTimeout, commandType: commandType);
        }
        public virtual TResult QuerySingleOrDefault<TResult>(string sql, object? param = null, int? commandTimeout = null, CommandType? commandType = null)
        {
            return _dbConnection.QuerySingleOrDefault<TResult>(sql: sql, param: param, transaction: _dbTransaction, commandTimeout: commandTimeout, commandType: commandType);
        }
        public virtual Task<TResult> QuerySingleOrDefaultAsync<TResult>(string sql, object? param = null, int? commandTimeout = null, CommandType? commandType = null)
        {
            return _dbConnection.QuerySingleOrDefaultAsync<TResult>(sql: sql, param: param, transaction: _dbTransaction, commandTimeout: commandTimeout, commandType: commandType);
        }

        public async Task<PageResult<TResult>> QueryPageAsync<TResult>(string sql, object? param = null, int? page = 1, int? limit = 20, int? commandTimeout = null, CommandType? commandType = null)
        {
            var sqlstr = $"SELECT SQL_CALC_FOUND_ROWS * FROM ({sql.TrimEnd(';')}) T LIMIT  {((page - 1) * limit)},{limit};SELECT FOUND_ROWS();";
            var query = await _dbConnection.QueryMultipleAsync(sqlstr, param: param, transaction: _dbTransaction, commandTimeout: commandTimeout, commandType: commandType);
            var item = await query.ReadAsync<TResult>();
            var count = await query.ReadSingleAsync<long>();
            return new PageResult<TResult>(item, count);
        }

        #endregion

        ~MysqlContext()
        {
            if (_dbConnection != null)
            {
                _dbConnection.Dispose();
                _dbConnection = null;
            }
        }
    }
}
