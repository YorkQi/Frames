using Frame.Core.Entitys;
using Frame.Repository.DataObjects.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Frame.Repository.DataObjects
{
    public static class DataObjectModelHandler
    {
        /// <summary>
        /// 实体对象转化成DataObjectModel对象
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <returns></returns>
        public static DataObjectTable ToTable<TEntity>(TEntity entity) where TEntity : IEntity
        {
            Type type = entity.GetType();
            var columns = new List<DataObjectColumn>();
            var keyColumn = new DataObjectColumn();
            var properties = type.GetProperties();

            foreach (var item in properties)
            {
                if (item.GetCustomAttributes(typeof(KeyAttribute), true).Any())
                {
                    keyColumn = new DataObjectColumn { Name = item.Name, Value = item.GetValue(entity) };
                    continue;
                }
                columns.Add(new DataObjectColumn { Name = item.Name, Value = item.GetValue(entity) });
            }
            return new DataObjectTable
            {
                Name = type.Name,
                KeyColumn = keyColumn,
                Columns = columns
            };
        }

        /// <summary>
        /// 多实体对象转化成DataObjectModel对象
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <returns></returns>
        public static List<DataObjectTable> ToTable<TEntity>(IEnumerable<TEntity> entitys) where TEntity : IEntity
        {
            var result = new List<DataObjectTable>();
            foreach (var entity in entitys)
            {
                result.Add(ToTable(entity));
            }
            return result;
        }
    }
}
