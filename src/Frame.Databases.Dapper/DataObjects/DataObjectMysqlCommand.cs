using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace Frame.Databases.Dapper.DataObjects
{
    public static class DataObjectMysqlCommand
    {
        public static string InsertToSql(DataObjectTable dataTable)
        {
            return $@" INSERT INTO {dataTable.Name}({string.Join(",", dataTable.Columns.Select(t => $"`{t.Name}`"))})VALUE({string.Join(",", dataTable.Columns.Select(t => $"@{t.Name}"))});SELECT @@IDENTITY;";
        }

        public static string InsertBatchToSql(List<DataObjectTable> dataTables)
        {
            var values = new StringBuilder();
            for (int i = 0; i < dataTables.Count(); i++)
            {
                values.Append($"({string.Join(",", dataTables[i].Columns.Select(t => $"@{t.Name}_{i}"))}),");
            }

            return $@" INSERT INTO {dataTables.First().Name}({string.Join(",", dataTables.First().Columns.Select(t => t.Name))})VALUES {values.ToString().TrimEnd(',')};";
        }


        public static string UpdateToSql(DataObjectTable dataTable)
        {
            return $@" UPDATE {dataTable.Name} SET {string.Join(",", dataTable.Columns.Select(t => $"`{t.Name}`=@{t.Name}"))} WHERE `{dataTable.KeyColumn.Name}`=@{dataTable.KeyColumn.Name};";
        }

        public static string UpdateBatchToSql(List<DataObjectTable> dataTables)
        {
            StringBuilder result = new StringBuilder();
            for (int i = 0; i < dataTables.Count(); i++)
            {
                result.Append($@" UPDATE {dataTables[i].Name} SET {string.Join(",", dataTables[i].Columns.Select(t => $"`{t.Name}`=@{t.Name}_{i}"))} WHERE `{dataTables[i].KeyColumn.Name}`=@{dataTables[i].KeyColumn.Name}_{i};");
            }

            return result.ToString();
        }
    }
}
