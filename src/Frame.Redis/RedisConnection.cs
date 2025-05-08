using System.Collections;
using System.Collections.Generic;

namespace Frame.Redis.Locks
{
    public class RedisConnection : IEnumerable<string>
    {
        private List<string> RedisStrs { get; set; } = new();

        public RedisConnection(params string[] conectionStr)
        {
            RedisStrs.AddRange(conectionStr);
        }
        public void Add(params string[] conectionStr)
        {
            RedisStrs.AddRange(conectionStr);
        }
        public void AddRange(IEnumerable<string> conectionStrs)
        {
            this.RedisStrs.AddRange(conectionStrs);
        }

        public IEnumerable<string> Get()
        {
            return RedisStrs;
        }

        public IEnumerator<string> GetEnumerator()
        {
            return RedisStrs.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return RedisStrs.GetEnumerator();
        }
    }
}
