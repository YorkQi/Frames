using Frame.Repository.DataObjects.Models;
using System.Collections.Generic;

namespace Frame.Repository.DataObjects
{
    public interface IDataObjectSqlHandler
    {
        string InsertToSql(DataObjectTable dataTable);

        string InsertBatchToSql(List<DataObjectTable> dataTables);

        string UpdateToSql(DataObjectTable dataTable);

        string UpdateBatchToSql(List<DataObjectTable> dataTables);
    }
}
