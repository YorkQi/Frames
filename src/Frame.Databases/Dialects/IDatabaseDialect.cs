namespace Frame.Databases.Dialects
{
    /// <summary>
    /// 数据库方言接口。抽象不同数据库之间的语法差异。
    /// </summary>
    public interface IDatabaseDialect
    {
        /// <summary>
        /// 构建分页查询 SQL。将原始 SQL 包装为一次返回 COUNT 和数据的分页查询。
        /// MySQL: <c>SELECT COUNT(*) FROM (sql) AS cnt; sql LIMIT {size} OFFSET {offset};</c>
        /// SQL Server: <c>SELECT COUNT(*) FROM (sql) AS cnt; sql OFFSET {offset} ROWS FETCH NEXT {size} ROWS ONLY;</c>
        /// PostgreSQL: <c>SELECT COUNT(*) FROM (sql) AS cnt; sql LIMIT {size} OFFSET {offset};</c>
        /// Oracle: <c>SELECT COUNT(*) FROM (sql); SELECT * FROM (SELECT ..., ROWNUM rn FROM ...) WHERE rn BETWEEN ...</c>
        /// </summary>
        /// <param name="baseSql">原始查询 SQL（不含分页子句）</param>
        /// <param name="page">页码，从 1 开始</param>
        /// <param name="size">每页条数</param>
        /// <returns>分页 SQL：第一行返回总条数，第二行返回当前页数据</returns>
        string BuildPagedSql(string baseSql, int page, int size);

        /// <summary>
        /// 获取分页参数。
        /// </summary>
        /// <param name="page">页码</param>
        /// <param name="size">每页条数</param>
        /// <param name="offset">输出的偏移量</param>
        /// <returns>SQL 参数对象</returns>
        object? GetPaginationParams(int page, int size, out int offset);

        /// <summary>
        /// 对表名或列名进行引用（防注入 + 适应大小写策略）。
        /// MySQL: <c>`table_name`</c>
        /// SQL Server: <c>[table_name]</c>
        /// PostgreSQL: <c>"table_name"</c>
        /// </summary>
        string QuoteName(string name);

        /// <summary>
        /// 对模式名和表名进行引用。模式名为 null 或空时仅引用表名。
        /// MySQL: <c>`schema`.`table`</c>
        /// SQL Server: <c>[schema].[table]</c>
        /// PostgreSQL: <c>"schema"."table"</c>
        /// </summary>
        string QuoteTableName(string? schema, string tableName);

        /// <summary>
        /// 格式化参数名称，添加参数前缀。
        /// MySQL/SQL Server: <c>@paramName</c>
        /// PostgreSQL: <c>:paramName</c> 或 <c>@paramName</c>
        /// Oracle: <c>:paramName</c>
        /// </summary>
        string FormatParameter(string parameterName);

        /// <summary>
        /// 数据库方言名称，用于日志和诊断。
        /// </summary>
        string Name { get; }
    }
}
