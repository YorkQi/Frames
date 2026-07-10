using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;

namespace Frame.Databases.DbContexts
{
    /// <summary>
    /// IDBContextReader 的 Dapper 实现，包装 Dapper 的 SqlMapper.GridReader。
    /// </summary>
    internal sealed class DBContextReader(SqlMapper.GridReader gridReader) : IDBContextReader
    {
        private readonly SqlMapper.GridReader _gridReader = gridReader ?? throw new ArgumentNullException(nameof(gridReader));
        private bool _disposed;

        public IEnumerable<T> Read<T>()
        {
            return _gridReader.Read<T>();
        }

        public Task<IEnumerable<T>> ReadAsync<T>()
        {
            return _gridReader.ReadAsync<T>();
        }

        public T ReadFirst<T>()
        {
            return _gridReader.ReadFirst<T>();
        }

        public Task<T> ReadFirstAsync<T>()
        {
            return _gridReader.ReadFirstAsync<T>();
        }

        public T? ReadFirstOrDefault<T>()
        {
            return _gridReader.ReadFirstOrDefault<T>();
        }

        public Task<T?> ReadFirstOrDefaultAsync<T>()
        {
#pragma warning disable CS8619 // Dapper 未标注可空性，包装层已知返回值可为 null
            return _gridReader.ReadFirstOrDefaultAsync<T>();
#pragma warning restore CS8619
        }

        public T ReadSingle<T>()
        {
            return _gridReader.ReadSingle<T>();
        }

        public Task<T> ReadSingleAsync<T>()
        {
            return _gridReader.ReadSingleAsync<T>();
        }

        public T? ReadSingleOrDefault<T>()
        {
            return _gridReader.ReadSingleOrDefault<T>();
        }

        public Task<T?> ReadSingleOrDefaultAsync<T>()
        {
#pragma warning disable CS8619 // Dapper 未标注可空性，包装层已知返回值可为 null
            return _gridReader.ReadSingleOrDefaultAsync<T>();
#pragma warning restore CS8619
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _gridReader.Dispose();
                _disposed = true;
            }
        }
    }
}
