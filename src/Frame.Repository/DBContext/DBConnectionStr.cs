using System.Collections;
using System.Collections.Generic;

namespace Frame.Repository.Context
{
    public class DBConnectionStr : IEnumerable<string>
    {
        private List<string> ConnectionStrs { get; set; } = new List<string>();

        public void Add(string conectionStr)
        {
            ConnectionStrs.Add(conectionStr);
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
