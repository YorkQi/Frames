using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Frame.Core.Locks
{
    public class LockConfig
    {
        public LockConfig([NotNull] IEnumerable<string> conections)
        {
            Check.NotNull(conections, nameof(conections));

            Conections = conections;
        }
        public IEnumerable<string> Conections { get; set; }
    }
}
