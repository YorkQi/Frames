using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
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
        // ===========================================================================
        // IDBContext Sql执行部分 实现 — 所有方法通过 Dapper 执行，事务自动传递
        // ===========================================================================

        public IEnumerable<T> Query<T>(string sql, object? param = null, CommandType? commandType = null, int? commandTimeout = null, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return GetDbConnection(cancellationToken).Query<T>(sql, param, transaction: GetDbTransaction(), commandType: commandType, commandTimeout: commandTimeout);
        }

        public async Task<IEnumerable<T>> QueryAsync<T>(string sql, object? param = null, CommandType? commandType = null, int? commandTimeout = null, CancellationToken cancellationToken = default)
        {
            return await GetDbConnection(cancellationToken).QueryAsync<T>(sql, param, transaction: GetDbTransaction(), commandType: commandType, commandTimeout: commandTimeout);
        }

        public T QueryFirst<T>(string sql, object? param = null, CommandType? commandType = null, int? commandTimeout = null, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return GetDbConnection(cancellationToken).QueryFirst<T>(sql, param, transaction: GetDbTransaction(), commandType: commandType, commandTimeout: commandTimeout);
        }

        public async Task<T> QueryFirstAsync<T>(string sql, object? param = null, CommandType? commandType = null, int? commandTimeout = null, CancellationToken cancellationToken = default)
        {
            return await GetDbConnection(cancellationToken).QueryFirstAsync<T>(sql, param, transaction: GetDbTransaction(), commandType: commandType, commandTimeout: commandTimeout);
        }

        public T? QueryFirstOrDefault<T>(string sql, object? param = null, CommandType? commandType = null, int? commandTimeout = null, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return GetDbConnection(cancellationToken).QueryFirstOrDefault<T>(sql, param, transaction: GetDbTransaction(), commandType: commandType, commandTimeout: commandTimeout);
        }

        public async Task<T?> QueryFirstOrDefaultAsync<T>(string sql, object? param = null, CommandType? commandType = null, int? commandTimeout = null, CancellationToken cancellationToken = default)
        {
            return await GetDbConnection(cancellationToken).QueryFirstOrDefaultAsync<T>(sql, param, transaction: GetDbTransaction(), commandType: commandType, commandTimeout: commandTimeout);
        }

        public T QuerySingle<T>(string sql, object? param = null, CommandType? commandType = null, int? commandTimeout = null, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return GetDbConnection(cancellationToken).QuerySingle<T>(sql, param, transaction: GetDbTransaction(), commandType: commandType, commandTimeout: commandTimeout);
        }

        public async Task<T> QuerySingleAsync<T>(string sql, object? param = null, CommandType? commandType = null, int? commandTimeout = null, CancellationToken cancellationToken = default)
        {
            return await GetDbConnection(cancellationToken).QuerySingleAsync<T>(sql, param, transaction: GetDbTransaction(), commandType: commandType, commandTimeout: commandTimeout);
        }

        public T? QuerySingleOrDefault<T>(string sql, object? param = null, CommandType? commandType = null, int? commandTimeout = null, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return GetDbConnection(cancellationToken).QuerySingleOrDefault<T>(sql, param, transaction: GetDbTransaction(), commandType: commandType, commandTimeout: commandTimeout);
        }

        public async Task<T?> QuerySingleOrDefaultAsync<T>(string sql, object? param = null, CommandType? commandType = null, int? commandTimeout = null, CancellationToken cancellationToken = default)
        {
            return await GetDbConnection(cancellationToken).QuerySingleOrDefaultAsync<T>(sql, param, transaction: GetDbTransaction(), commandType: commandType, commandTimeout: commandTimeout);
        }

        public int Execute(string sql, object? param = null, CommandType? commandType = null, int? commandTimeout = null, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return GetDbConnection(cancellationToken).Execute(sql, param, transaction: GetDbTransaction(), commandType: commandType, commandTimeout: commandTimeout);
        }

        public async Task<int> ExecuteAsync(string sql, object? param = null, CommandType? commandType = null, int? commandTimeout = null, CancellationToken cancellationToken = default)
        {
            return await GetDbConnection(cancellationToken).ExecuteAsync(sql, param, transaction: GetDbTransaction(), commandType: commandType, commandTimeout: commandTimeout);
        }

        public T ExecuteScalar<T>(string sql, object? param = null, CommandType? commandType = null, int? commandTimeout = null, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return GetDbConnection(cancellationToken).ExecuteScalar<T>(sql, param, transaction: GetDbTransaction(), commandType: commandType, commandTimeout: commandTimeout);
        }

        public async Task<T> ExecuteScalarAsync<T>(string sql, object? param = null, CommandType? commandType = null, int? commandTimeout = null, CancellationToken cancellationToken = default)
        {
            return await GetDbConnection(cancellationToken).ExecuteScalarAsync<T>(sql, param, transaction: GetDbTransaction(), commandType: commandType, commandTimeout: commandTimeout);
        }

        public IDBContextReader QueryMultiple(string sql, object? param = null, CommandType? commandType = null, int? commandTimeout = null, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var gridReader = GetDbConnection(cancellationToken).QueryMultiple(sql, param, transaction: GetDbTransaction(), commandType: commandType, commandTimeout: commandTimeout);
            return new DBContextReader(gridReader);
        }

        public async Task<IDBContextReader> QueryMultipleAsync(string sql, object? param = null, CommandType? commandType = null, int? commandTimeout = null, CancellationToken cancellationToken = default)
        {
            var gridReader = await GetDbConnection(cancellationToken).QueryMultipleAsync(sql, param, transaction: GetDbTransaction(), commandType: commandType, commandTimeout: commandTimeout);
            return new DBContextReader(gridReader);
        }
    }
}
