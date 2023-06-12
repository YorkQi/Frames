using Frame.Repository.DataObjects;
using Frame.Repository.DataObjects.Models;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Frame.Repository.Mysql.DataObjectModel
{
    public class DataObjectSqlHandler : IDataObjectSqlHandler
    {
        public string InsertToSql(DataObjectTable dataTable)
        {
            var columns = string.Join(',', dataTable.Columns.Select(t => $"`{t.Name}`"));
            var values = string.Join(',', dataTable.Columns.Select(t => $"@{t.Name}"));
            return $@" INSERT INTO {dataTable.Name}({columns})VALUE({values});SELECT @@IDENTITY;";
        }

        public string InsertBatchToSql(List<DataObjectTable> dataTables)
        {
            var columns = string.Join(',', dataTables.First().Columns.Select(t => t.Name));
            var values = new StringBuilder();
            for (int i = 0; i < dataTables.Count(); i++)
            {
                values.Append($"({string.Join(',', dataTables[i].Columns.Select(t => $"@{t.Name}_{i}"))}),");
            }

            return $@" INSERT INTO {dataTables.First().Name}({columns})VALUES {values.ToString().TrimEnd(',')};";
        }


        public string UpdateToSql(DataObjectTable dataTable)
        {
            var columnValues = string.Join(',', dataTable.Columns.Select(t => $"`{t.Name}`=@{t.Name}"));
            return $@" UPDATE {dataTable.Name} SET {columnValues} WHERE `{dataTable.KeyColumn.Name}`=@{dataTable.KeyColumn.Name};";
        }

        public string UpdateBatchToSql(List<DataObjectTable> dataTables)
        {
            StringBuilder result = new StringBuilder();
            for (int i = 0; i < dataTables.Count(); i++)
            {
                result.Append($@" UPDATE {dataTables[i].Name} SET {string.Join(',', dataTables[i].Columns.Select(t => $"`{t.Name}`=@{t.Name}_{i}"))} WHERE `{dataTables[i].KeyColumn.Name}`=@{dataTables[i].KeyColumn.Name}_{i};");
            }

            return result.ToString();
        }
    }
}
