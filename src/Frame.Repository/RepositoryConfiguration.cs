using System;

namespace Frame.Repository
{
    internal class RepositoryConfiguration
    {

        internal Type? TypeKey { get; set; }

        internal Func<IServiceProvider, object>? TypeValue { get; set; }
    }
}
