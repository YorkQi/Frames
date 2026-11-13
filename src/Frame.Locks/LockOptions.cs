using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Frame.Locks
{
    public class LockOptions
    {
        public LockOptions([NotNull] IEnumerable<string> connections)
        {
            if (connections is null) throw new ArgumentNullException(nameof(connections));

            Connections = connections;
        }

        public IEnumerable<string> Connections { get; set; }
    }
}
