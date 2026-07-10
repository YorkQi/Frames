using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Frame.Core.Utils;

namespace Frame.Locks
{
    public class LockOptions
    {
        public LockOptions([NotNull] IEnumerable<string> connections)
        {
            Check.NotNull(connections, nameof(connections));

            Connections = connections;
        }

        public IEnumerable<string> Connections { get; set; }
    }
}
