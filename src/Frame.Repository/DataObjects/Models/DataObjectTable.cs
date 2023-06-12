using System.Collections.Generic;

namespace Frame.Repository.DataObjects.Models
{
    public class DataObjectTable
    {
        /// <summary>
        /// 表名
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 主键
        /// </summary>
        public DataObjectColumn KeyColumn { get; set; } = new DataObjectColumn();

        /// <summary>
        /// 列名集合
        /// </summary>
        public IEnumerable<DataObjectColumn> Columns { get; set; } = new List<DataObjectColumn>();

    }
}
