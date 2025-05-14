using System;
using System.Diagnostics.CodeAnalysis;

namespace Frame.Core
{
    public class ServiceConfigurationBuilder
    {
        private Action<ServiceConfigurationContext> configuration;
        internal ServiceConfigurationBuilder()
        {
            configuration ??= descriptor => { };
        }

        public ServiceConfigurationBuilder Add([NotNull] Action<ServiceConfigurationContext> action)
        {
            Check.NotNull(action, nameof(action));

            configuration += action;
            return this;
        }

        public Action<ServiceConfigurationContext> Build() => configuration;
    }
}
