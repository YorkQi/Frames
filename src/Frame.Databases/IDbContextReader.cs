using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Frame.Databases
{
    /// <summary>
    /// 多结果集读取器。由 QueryMultiple / QueryMultipleAsync 返回，
    /// 用于依次读取多个 SQL 查询的结果集。
    /// </summary>
    public interface IDBContextReader : IDisposable
    {
        /// <summary>读取当前结果集的所有行</summary>
        IEnumerable<T> Read<T>();

        /// <summary>异步读取当前结果集的所有行</summary>
        Task<IEnumerable<T>> ReadAsync<T>();

        /// <summary>读取当前结果集的第一行（无结果时抛异常）</summary>
        T ReadFirst<T>();

        /// <summary>异步读取当前结果集的第一行（无结果时抛异常）</summary>
        Task<T> ReadFirstAsync<T>();

        /// <summary>读取当前结果集的第一行或默认值</summary>
        T? ReadFirstOrDefault<T>();

        /// <summary>异步读取当前结果集的第一行或默认值</summary>
        Task<T?> ReadFirstOrDefaultAsync<T>();

        /// <summary>读取当前结果集的单行（多行或无结果时抛异常）</summary>
        T ReadSingle<T>();

        /// <summary>异步读取当前结果集的单行（多行或无结果时抛异常）</summary>
        Task<T> ReadSingleAsync<T>();

        /// <summary>读取当前结果集的单行或默认值（多行时抛异常）</summary>
        T? ReadSingleOrDefault<T>();

        /// <summary>异步读取当前结果集的单行或默认值（多行时抛异常）</summary>
        Task<T?> ReadSingleOrDefaultAsync<T>();
    }
}
