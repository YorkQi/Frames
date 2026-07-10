using System.Collections.Generic;

namespace Frame.Core.Entities.Dtos
{
    public class PageResult<TEntity>(IEnumerable<TEntity> items, long count)
    {
        public IEnumerable<TEntity> Items { get; private set; } = items;

        public long Count { get; private set; } = count;

    }
}
