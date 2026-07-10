using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Dapper;

namespace Frame.Databases
{
    /// <summary>
    /// Frame 框架的统一数据库参数集合。
    /// 支持动态追加参数、条件参数、Output/Return 参数，
    /// 执行后可通过 <see cref="Get{T}"/> 读回 Output 值。
    /// </summary>
    /// <remarks>
    /// <para><b>简单场景</b> — 匿名对象直接传入（与旧写法一致）：</para>
    /// <code>
    /// var users = await DB.QueryAsync&lt;User&gt;(
    ///     "SELECT * FROM Users WHERE Name LIKE @Name",
    ///     new DbParameters(new { Name = $"%{name}%" }));
    /// </code>
    /// <para><b>动态拼接参数</b> — 条件 WHERE：</para>
    /// <code>
    /// var p = new DbParameters()
    ///     .Add("Name", $"%{name}%")
    ///     .AddIf(status.HasValue, "Status", status.Value)
    ///     .AddIf(createdAfter.HasValue, "CreatedAt", createdAfter.Value);
    /// var users = await DB.QueryAsync&lt;User&gt;("SELECT * FROM Users WHERE 1=1 ...", p);
    /// </code>
    /// <para><b>存储过程 + Output 参数</b>：</para>
    /// <code>
    /// var p = new DbParameters()
    ///     .Add("Name", $"%{name}%")
    ///     .AddOutput("TotalCount", DbType.Int32);
    /// var users = await DB.QueryAsync&lt;User&gt;("sp_SearchUsers", p,
    ///     commandType: CommandType.StoredProcedure);
    /// var total = p.Get&lt;int&gt;("TotalCount");
    /// </code>
    /// </remarks>
    public sealed class DbParameters
    {
        private readonly List<ParamEntry> _entries = new();
        private DynamicParameters? _dapperParams;

        // ===========================================================================
        // 构造
        // ===========================================================================

        /// <summary>创建空参数集合</summary>
        public DbParameters() { }

        /// <summary>
        /// 从现有对象创建，反射读取属性名作为参数名、属性值作为参数值。
        /// 兼容匿名对象、DTO 等。
        /// </summary>
        /// <param name="parameters">匿名对象或任意包含属性的对象</param>
        /// <exception cref="ArgumentNullException"><paramref name="parameters"/> 为 null</exception>
        public DbParameters(object parameters)
        {
            if (parameters is null)
                throw new ArgumentNullException(nameof(parameters));

            foreach (var prop in parameters.GetType().GetProperties())
            {
                Add(prop.Name, prop.GetValue(parameters));
            }
        }

        /// <summary>静态工厂：从现有对象创建参数集合</summary>
        /// <param name="parameters">匿名对象或任意包含属性的对象</param>
        /// <returns>新的 DbParameters 实例</returns>
        public static DbParameters From(object parameters) => new(parameters);

        // ===========================================================================
        // 添加参数（流畅 API，均返回 this 以支持链式调用）
        // ===========================================================================

        /// <summary>添加一个命名参数</summary>
        /// <param name="name">参数名（无需 @ 前缀，框架自动处理）</param>
        /// <param name="value">参数值</param>
        /// <param name="dbType">数据库类型（可选，框架按需推断）</param>
        /// <param name="direction">参数方向：Input / Output / ReturnValue</param>
        /// <param name="size">大小（适用于字符串、字节数组）</param>
        /// <returns>当前实例，支持链式调用</returns>
        /// <exception cref="ArgumentException"><paramref name="name"/> 为空</exception>
        public DbParameters Add(string name, object? value,
            DbType? dbType = null,
            ParameterDirection direction = ParameterDirection.Input,
            int? size = null)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("参数名不能为空", nameof(name));

            _dapperParams = null;
            _entries.Add(new ParamEntry(name, value, dbType, direction, size));
            return this;
        }

        /// <summary>
        /// 条件追加：仅当 <paramref name="condition"/> 为 <c>true</c> 时添加参数。
        /// 用于动态 WHERE 条件，避免写 if-else 分支。
        /// </summary>
        /// <example>
        /// <code>
        /// var p = new DbParameters()
        ///     .Add("Name", $"%{name}%")
        ///     .AddIf(status.HasValue, "Status", status.Value)
        ///     .AddIf(tagIds?.Any() == true, "TagIds", tagIds);
        /// </code>
        /// </example>
        /// <returns>当前实例，支持链式调用</returns>
        public DbParameters AddIf(bool condition, string name, object? value,
            DbType? dbType = null,
            ParameterDirection direction = ParameterDirection.Input,
            int? size = null)
        {
            if (condition)
                return Add(name, value, dbType, direction, size);
            return this;
        }

        /// <summary>
        /// 添加 Output 参数。执行存储过程后通过 <see cref="Get{T}"/> 读回输出值。
        /// </summary>
        /// <param name="name">参数名</param>
        /// <param name="dbType">数据库类型（必填，Output 参数必须指定类型）</param>
        /// <param name="size">大小（可选）</param>
        /// <returns>当前实例，支持链式调用</returns>
        public DbParameters AddOutput(string name, DbType dbType, int? size = null)
        {
            return Add(name, null, dbType, ParameterDirection.Output, size);
        }

        /// <summary>
        /// 添加返回值参数（对应存储过程的 RETURN 语句）。
        /// 执行后可调用 <c>Get&lt;int&gt;("returnValue")</c> 读取。
        /// </summary>
        /// <param name="name">参数名，默认 "returnValue"</param>
        /// <param name="dbType">数据库类型，默认 Int32</param>
        /// <returns>当前实例，支持链式调用</returns>
        public DbParameters AddReturn(string name = "returnValue", DbType dbType = DbType.Int32)
        {
            return Add(name, null, dbType, ParameterDirection.ReturnValue, null);
        }

        // ===========================================================================
        // 读取回值
        // ===========================================================================

        /// <summary>
        /// 获取 Output / Return 参数的执行后值。
        /// 必须在 SQL 执行后调用，否则抛异常。
        /// </summary>
        /// <typeparam name="T">返回值类型</typeparam>
        /// <param name="name">参数名</param>
        /// <returns>输出值</returns>
        /// <exception cref="InvalidOperationException">尚未执行 SQL，无法读取输出值</exception>
        public T? Get<T>(string name)
        {
            if (_dapperParams is null)
                throw new InvalidOperationException(
                    "参数尚未执行，无法读取输出值。请先通过 IDbContext 执行 SQL（如 ExecuteAsync / QueryAsync）。");

            return _dapperParams.Get<T>(name);
        }

        /// <summary>
        /// 尝试获取 Output / Return 参数的执行后值。
        /// </summary>
        /// <returns><c>true</c> 表示成功读取到值</returns>
        public bool TryGet<T>(string name, out T? value)
        {
            value = default;

            if (_dapperParams is null) return false;

            try
            {
                value = _dapperParams.Get<T>(name);
                return true;
            }
            catch
            {
                return false;
            }
        }

        // ===========================================================================
        // 集合操作
        // ===========================================================================

        /// <summary>当前已添加的参数数量</summary>
        public int Count => _entries.Count;

        /// <summary>检查是否已包含指定名称的参数</summary>
        public bool Contains(string name)
        {
            return _entries.Exists(e => e.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>移除指定名称的参数（不存在时不报错）</summary>
        /// <returns>当前实例，支持链式调用</returns>
        public DbParameters Remove(string name)
        {
            _dapperParams = null;
            _entries.RemoveAll(e => e.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            return this;
        }

        /// <summary>清空所有已添加的参数</summary>
        /// <returns>当前实例，支持链式调用</returns>
        public DbParameters Clear()
        {
            _dapperParams = null;
            _entries.Clear();
            return this;
        }

        /// <summary>将另一个 DbParameters 的所有参数合并到当前实例（不改变对方）</summary>
        /// <returns>当前实例，支持链式调用</returns>
        public DbParameters Merge(DbParameters other)
        {
            if (other is null)
                throw new ArgumentNullException(nameof(other));

            _dapperParams = null;
            _entries.AddRange(other._entries);
            return this;
        }

        // ===========================================================================
        // Dapper 桥接（框架内部使用，非公开 API）
        // ===========================================================================

        /// <summary>
        /// 转为 Dapper DynamicParameters，同时保留引用以支持 Output 读回。
        /// </summary>
        internal DynamicParameters ToDynamicParameters()
        {
            var dp = new DynamicParameters();
            foreach (var entry in _entries)
            {
                dp.Add(entry.Name, entry.Value, entry.DbType, entry.Direction, entry.Size);
            }
            _dapperParams = dp;
            return dp;
        }

        // ===========================================================================
        // 内部数据结构
        // ===========================================================================

        /// <summary>单个参数定义</summary>
        private sealed class ParamEntry
        {
            public string Name { get; }
            public object? Value { get; }
            public DbType? DbType { get; }
            public ParameterDirection Direction { get; }
            public int? Size { get; }

            public ParamEntry(string name, object? value, DbType? dbType, ParameterDirection direction, int? size)
            {
                Name = name;
                Value = value;
                DbType = dbType;
                Direction = direction;
                Size = size;
            }
        }
    }
}
