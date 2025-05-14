using System.Collections.Generic;

namespace Frame.Databases.Entitys.Dtos
{
    public class PageResult<TEntity>
    {
        public PageResult(IEnumerable<TEntity> items, long count)
        {
            Items = items;
            Count = count;
        }

        public IEnumerable<TEntity> Items { get; private set; }

        public long Count { get; private set; }

    }
}
