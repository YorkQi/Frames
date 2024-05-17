using System.Collections;
using System.Collections.Generic;

namespace Frame.Repository.DBContexts
{
    public class DBConnectionString : IEnumerable<string>
    {
        public DBConnectionString(params string[] conectionStr)
        {
            ConnectionStrs.AddRange(conectionStr);
        }
        private List<string> ConnectionStrs { get; set; } = new List<string>();

        public void Add(params string[] conectionStr)
        {
            ConnectionStrs.AddRange(conectionStr);
        }
        public void AddRange(IEnumerable<string> conectionStrs)
        {
            ConnectionStrs.AddRange(conectionStrs);
        }

        public IEnumerable<string> Get()
        {
            return ConnectionStrs;
        }

        public IEnumerator<string> GetEnumerator()
        {
            return ConnectionStrs.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ConnectionStrs.GetEnumerator();
        }
    }
}
