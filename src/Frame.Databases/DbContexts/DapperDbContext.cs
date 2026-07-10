using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using Frame.Core.Entities.Dtos;
using Frame.Databases.DbContexts;
using Microsoft.EntityFrameworkCore;

namespace Frame.Databases.DBContext
{
    /// <summary>
    /// IDBContext 实现。
    /// 原始 SQL 通过 GetDbConnection() 获取连接后直接使用 Dapper 扩展。
    /// </summary>
    public partial class DbContext : IDbContext, IDisposable, IAsyncDisposable
    {
        /// <summary>
        /// 将框架的 <see cref="DbParameters"/> 转为 Dapper 可消费的参数格式。
        /// 匿名对象、DynamicParameters 等其他类型原样透传。
        /// </summary>
        private static object? ToDapperParam(object? param)
        {
            if (param is DbParameters dbParams)
                return dbParams.ToDynamicParameters();
            return param;
        }

        // ===========================================================================
        // IDBContext Sql执行部分 实现 — 所有方法通过 Dapper 执行，事务自动传递
        // ===========================================================================

        public IEnumerable<T> Query<T>(string sql, object? param = null, CommandType? commandType = null, int? commandTimeout = null, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return GetDbConnection(cancellationToken).Query<T>(sql, ToDapperParam(param), transaction: GetDbTransaction(), commandType: commandType, commandTimeout: commandTimeout);
        }

        public async Task<IEnumerable<T>> QueryAsync<T>(string sql, object? param = null, CommandType? commandType = null, int? commandTimeout = null, CancellationToken cancellationToken = default)
        {
            return await GetDbConnection(cancellationToken).QueryAsync<T>(sql, ToDapperParam(param), transaction: GetDbTransaction(), commandType: commandType, commandTimeout: commandTimeout);
        }

        public T QueryFirst<T>(string sql, object? param = null, CommandType? commandType = null, int? commandTimeout = null, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return GetDbConnection(cancellationToken).QueryFirst<T>(sql, ToDapperParam(param), transaction: GetDbTransaction(), commandType: commandType, commandTimeout: commandTimeout);
        }

        public async Task<T> QueryFirstAsync<T>(string sql, object? param = null, CommandType? commandType = null, int? commandTimeout = null, CancellationToken cancellationToken = default)
        {
            return await GetDbConnection(cancellationToken).QueryFirstAsync<T>(sql, ToDapperParam(param), transaction: GetDbTransaction(), commandType: commandType, commandTimeout: commandTimeout);
        }

        public T? QueryFirstOrDefault<T>(string sql, object? param = null, CommandType? commandType = null, int? commandTimeout = null, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return GetDbConnection(cancellationToken).QueryFirstOrDefault<T>(sql, ToDapperParam(param), transaction: GetDbTransaction(), commandType: commandType, commandTimeout: commandTimeout);
        }

        public async Task<T?> QueryFirstOrDefaultAsync<T>(string sql, object? param = null, CommandType? commandType = null, int? commandTimeout = null, CancellationToken cancellationToken = default)
        {
            return await GetDbConnection(cancellationToken).QueryFirstOrDefaultAsync<T>(sql, ToDapperParam(param), transaction: GetDbTransaction(), commandType: commandType, commandTimeout: commandTimeout);
        }

        public T QuerySingle<T>(string sql, object? param = null, CommandType? commandType = null, int? commandTimeout = null, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return GetDbConnection(cancellationToken).QuerySingle<T>(sql, ToDapperParam(param), transaction: GetDbTransaction(), commandType: commandType, commandTimeout: commandTimeout);
        }

        public async Task<T> QuerySingleAsync<T>(string sql, object? param = null, CommandType? commandType = null, int? commandTimeout = null, CancellationToken cancellationToken = default)
        {
            return await GetDbConnection(cancellationToken).QuerySingleAsync<T>(sql, ToDapperParam(param), transaction: GetDbTransaction(), commandType: commandType, commandTimeout: commandTimeout);
        }

        public T? QuerySingleOrDefault<T>(string sql, object? param = null, CommandType? commandType = null, int? commandTimeout = null, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return GetDbConnection(cancellationToken).QuerySingleOrDefault<T>(sql, ToDapperParam(param), transaction: GetDbTransaction(), commandType: commandType, commandTimeout: commandTimeout);
        }

        public async Task<T?> QuerySingleOrDefaultAsync<T>(string sql, object? param = null, CommandType? commandType = null, int? commandTimeout = null, CancellationToken cancellationToken = default)
        {
            return await GetDbConnection(cancellationToken).QuerySingleOrDefaultAsync<T>(sql, ToDapperParam(param), transaction: GetDbTransaction(), commandType: commandType, commandTimeout: commandTimeout);
        }

        public int Execute(string sql, object? param = null, CommandType? commandType = null, int? commandTimeout = null, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return GetDbConnection(cancellationToken).Execute(sql, ToDapperParam(param), transaction: GetDbTransaction(), commandType: commandType, commandTimeout: commandTimeout);
        }

        public async Task<int> ExecuteAsync(string sql, object? param = null, CommandType? commandType = null, int? commandTimeout = null, CancellationToken cancellationToken = default)
        {
            return await GetDbConnection(cancellationToken).ExecuteAsync(sql, ToDapperParam(param), transaction: GetDbTransaction(), commandType: commandType, commandTimeout: commandTimeout);
        }

        public T ExecuteScalar<T>(string sql, object? param = null, CommandType? commandType = null, int? commandTimeout = null, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return GetDbConnection(cancellationToken).ExecuteScalar<T>(sql, ToDapperParam(param), transaction: GetDbTransaction(), commandType: commandType, commandTimeout: commandTimeout);
        }

        public async Task<T> ExecuteScalarAsync<T>(string sql, object? param = null, CommandType? commandType = null, int? commandTimeout = null, CancellationToken cancellationToken = default)
        {
            return await GetDbConnection(cancellationToken).ExecuteScalarAsync<T>(sql, ToDapperParam(param), transaction: GetDbTransaction(), commandType: commandType, commandTimeout: commandTimeout);
        }

        public IDBContextReader QueryMultiple(string sql, object? param = null, CommandType? commandType = null, int? commandTimeout = null, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var gridReader = GetDbConnection(cancellationToken).QueryMultiple(sql, ToDapperParam(param), transaction: GetDbTransaction(), commandType: commandType, commandTimeout: commandTimeout);
            return new DBContextReader(gridReader);
        }

        public async Task<IDBContextReader> QueryMultipleAsync(string sql, object? param = null, CommandType? commandType = null, int? commandTimeout = null, CancellationToken cancellationToken = default)
        {
            var gridReader = await GetDbConnection(cancellationToken).QueryMultipleAsync(sql, ToDapperParam(param), transaction: GetDbTransaction(), commandType: commandType, commandTimeout: commandTimeout);
            return new DBContextReader(gridReader);
        }

        // ===========================================================================
        // 分页查询 — COUNT 子查询 + 数据，QueryMultiple 一趟往返
        // ===========================================================================

        public PageResult<T> QueryPaged<T>(string sql, object? param = null, int page = 1, int size = 20, CommandType? commandType = null, int? commandTimeout = null, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var (combinedSql, dapperParam) = BuildPagedSql(sql, param, page, size);

            using var reader = QueryMultiple(combinedSql, dapperParam, commandType, commandTimeout, cancellationToken);
            var total = reader.ReadSingle<long>();
            var items = reader.Read<T>().ToList();
            return new PageResult<T>(items, total);
        }

        public async Task<PageResult<T>> QueryPagedAsync<T>(string sql, object? param = null, int page = 1, int size = 20, CommandType? commandType = null, int? commandTimeout = null, CancellationToken cancellationToken = default)
        {
            var (combinedSql, dapperParam) = BuildPagedSql(sql, param, page, size);

            using var reader = await QueryMultipleAsync(combinedSql, dapperParam, commandType, commandTimeout, cancellationToken);
            var total = await reader.ReadSingleAsync<long>();
            var items = (await reader.ReadAsync<T>()).ToList();
            return new PageResult<T>(items, total);
        }

        private static (string sql, object? param) BuildPagedSql(string sql, object? param, int page, int size)
        {
            if (page < 1)
                throw new ArgumentOutOfRangeException(nameof(page), "页码必须 >= 1");
            if (size < 1)
                throw new ArgumentOutOfRangeException(nameof(size), "每页条数必须 >= 1");

            var offset = (page - 1) * size;
            var cleanSql = sql.TrimEnd(' ', ';', '\t', '\n', '\r');

            // COUNT 先执行，走索引覆盖；数据查询独立走 LIMIT/OFFSET，互不拖累
            var combined = $"SELECT COUNT(*) FROM ({cleanSql}) AS __cnt;\r\n{cleanSql} LIMIT {size} OFFSET {offset};";
            return (combined, param);
        }
    }
}
