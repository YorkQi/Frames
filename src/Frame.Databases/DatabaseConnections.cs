using Frame.Core;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Frame.Databases
{
    public class DatabaseConnections : IEnumerable<string>
    {
        [NotNull]
        private List<string> Connections { get; set; }

        public DatabaseConnections([NotNull] params string[] conections)
        {
            Check.NotNull(conections, nameof(conections));
            Connections = [.. conections];
        }

        public void Add(params string[] conectionStr)
        {
            Connections.AddRange(conectionStr);
        }
        public void AddRange(IEnumerable<string> conectionStrs)
        {
            Connections.AddRange(conectionStrs);
        }

        public IEnumerable<string> Get()
        {
            return Connections;
        }

        public IEnumerator<string> GetEnumerator()
        {
            return Connections.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return Connections.GetEnumerator();
        }
    }
}
