using System;

namespace Frame.Databases.Dialects
{
    /// <summary>
    /// SQL Server 数据库方言实现。
    /// 使用方括号引用标识符，OFFSET...FETCH NEXT 分页。
    /// 兼容：SQL Server 2012+（引入了 OFFSET/FETCH NEXT 语法）、Azure SQL Database
    /// </summary>
    public class SqlServerDialect : IDatabaseDialect
    {
        /// <summary>SQL Server 方言名称</summary>
        public string Name => "SQL Server";

        /// <summary>
        /// 构建 SQL Server 分页 SQL。
        /// 使用 <c>OFFSET {offset} ROWS FETCH NEXT {size} ROWS ONLY</c> 语法。
        /// 注意：原 SQL 必须包含 <c>ORDER BY</c> 子句，否则 OFFSET/FETCH 会报错。
        /// 建议用户在编写分页查询时始终包含 ORDER BY。
        /// </summary>
        public string BuildPagedSql(string baseSql, int page, int size)
        {
            if (page < 1) throw new ArgumentOutOfRangeException(nameof(page), "页码必须 >= 1");
            if (size < 1) throw new ArgumentOutOfRangeException(nameof(size), "每页条数必须 >= 1");

            var offset = (page - 1) * size;
            var cleanSql = baseSql.TrimEnd(' ', ';', '\t', '\n', '\r');

            // 移除末尾可能存在的 ORDER BY（用于 COUNT 查询时不需排序）
            // 注意：这种简单的移除不够健壮，但覆盖了大部分常见场景
            var countSql = RemoveOrderByClause(cleanSql);
            return $"SELECT COUNT(*) FROM ({countSql}) AS __cnt;\r\n{cleanSql} OFFSET {offset} ROWS FETCH NEXT {size} ROWS ONLY;";
        }

        /// <summary>SQL Server 分页参数：无需额外参数，OFFSET/FETCH 直接拼入 SQL</summary>
        public object? GetPaginationParams(int page, int size, out int offset)
        {
            offset = (page - 1) * size;
            return null;
        }

        /// <summary>SQL Server 使用方括号引用：`[column]`</summary>
        public string QuoteName(string name) => $"[{name}]";

        /// <summary>SQL Server 使用方括号引用表名：`[schema].[table]`</summary>
        public string QuoteTableName(string? schema, string tableName)
        {
            return string.IsNullOrWhiteSpace(schema)
                ? $"[{tableName}]"
                : $"[{schema}].[{tableName}]";
        }

        /// <summary>SQL Server 使用 @ 作为参数前缀</summary>
        public string FormatParameter(string parameterName) => $"@{parameterName}";

        /// <summary>简单移除 ORDER BY 子句（用于 COUNT 查询时优化）</summary>
        private static string RemoveOrderByClause(string sql)
        {
            // 查找最后一个 ORDER BY（不在子查询内）
            // 简单实现：不考虑嵌套子查询的情况
            var upper = sql.ToUpperInvariant();
            var lastOrderBy = upper.LastIndexOf("ORDER BY", StringComparison.Ordinal);
            if (lastOrderBy < 0) return sql;

            // 检查 ORDER BY 后是否有匹配的括号（避免误判子查询中的 ORDER BY）
            // 简单起见，先假设用户 SQL 最外层以 ORDER BY 结尾
            return sql[..lastOrderBy];
        }
    }
}
