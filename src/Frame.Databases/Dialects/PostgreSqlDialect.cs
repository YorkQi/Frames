using System;

namespace Frame.Databases.Dialects
{
    /// <summary>
    /// PostgreSQL 数据库方言实现。
    /// 使用双引号引用标识符，LIMIT/OFFSET 分页。
    /// 兼容：PostgreSQL 9.6+、Npgsql.EntityFrameworkCore.PostgreSQL
    /// </summary>
    public class PostgreSqlDialect : IDatabaseDialect
    {
        /// <summary>PostgreSQL 方言名称</summary>
        public string Name => "PostgreSQL";

        /// <summary>
        /// 构建 PostgreSQL 分页 SQL。
        /// PostgreSQL 与 MySQL 分页语法相同（LIMIT/OFFSET），
        /// 但标识符引用使用双引号而非反引号。
        /// </summary>
        public string BuildPagedSql(string baseSql, int page, int size)
        {
            if (page < 1) throw new ArgumentOutOfRangeException(nameof(page), "页码必须 >= 1");
            if (size < 1) throw new ArgumentOutOfRangeException(nameof(size), "每页条数必须 >= 1");

            var offset = (page - 1) * size;
            var cleanSql = baseSql.TrimEnd(' ', ';', '\t', '\n', '\r');

            return $"SELECT COUNT(*) FROM ({cleanSql}) AS __cnt;\r\n{cleanSql} LIMIT {size} OFFSET {offset};";
        }

        /// <summary>PostgreSQL 分页参数：无需额外参数，LIMIT/OFFSET 直接拼入 SQL</summary>
        public object? GetPaginationParams(int page, int size, out int offset)
        {
            offset = (page - 1) * size;
            return null;
        }

        /// <summary>PostgreSQL 使用双引号引用："column"</summary>
        public string QuoteName(string name) => $"\"{name}\"";

        /// <summary>PostgreSQL 双引号引用表名："schema"."table"</summary>
        public string QuoteTableName(string? schema, string tableName)
        {
            return string.IsNullOrWhiteSpace(schema)
                ? $"\"{tableName}\""
                : $"\"{schema}\".\"{tableName}\"";
        }

        /// <summary>
        /// PostgreSQL 兼容 @ 参数前缀（Npgsql 同时支持 :param 和 @param，建议使用 @ 以保持与 Dapper 的兼容性）
        /// </summary>
        public string FormatParameter(string parameterName) => $"@{parameterName}";
    }
}
