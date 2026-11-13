using System;

namespace Frame.Databases.Dialects
{
    /// <summary>
    /// MySQL 数据库方言实现。
    /// 使用反引号引用标识符，LIMIT/OFFSET 分页。
    /// 兼容：MySQL 5.7+、MariaDB 10+、Pomelo.EntityFrameworkCore.MySql
    /// </summary>
    public class MySqlDialect : IDatabaseDialect
    {
        /// <summary>MySQL 方言名称</summary>
        public string Name => "MySQL";

        /// <summary>
        /// 构建 MySQL 分页 SQL。
        /// 使用子查询 COUNT + LIMIT/OFFSET 分页。
        /// </summary>
        public string BuildPagedSql(string baseSql, int page, int size)
        {
            if (page < 1) throw new ArgumentOutOfRangeException(nameof(page), "页码必须 >= 1");
            if (size < 1) throw new ArgumentOutOfRangeException(nameof(size), "每页条数必须 >= 1");

            var offset = (page - 1) * size;
            var cleanSql = baseSql.TrimEnd(' ', ';', '\t', '\n', '\r');

            // 第一个 SELECT COUNT(*) 走索引覆盖，第二个查询带分页
            return $"SELECT COUNT(*) FROM ({cleanSql}) AS __cnt;\r\n{cleanSql} LIMIT {size} OFFSET {offset};";
        }

        /// <summary>MySQL 分页参数：无需额外参数，LIMIT/OFFSET 直接拼入 SQL</summary>
        public object? GetPaginationParams(int page, int size, out int offset)
        {
            offset = (page - 1) * size;
            return null; // LIMIT/OFFSET 已内联到 SQL
        }

        /// <summary>MySQL 使用反引号引用标识符：`column`</summary>
        public string QuoteName(string name) => $"`{name}`";

        /// <summary>MySQL 使用反引号引用表名：`schema`.`table` 或 `table`</summary>
        public string QuoteTableName(string? schema, string tableName)
        {
            return string.IsNullOrWhiteSpace(schema)
                ? $"`{tableName}`"
                : $"`{schema}`.`{tableName}`";
        }

        /// <summary>MySQL 使用 @ 作为参数前缀</summary>
        public string FormatParameter(string parameterName) => $"@{parameterName}";
    }
}
