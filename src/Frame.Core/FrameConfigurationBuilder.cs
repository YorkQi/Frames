using System;

namespace Frame.Core.FrameModules
{
    public class FrameConfigurationBuilder
    {
        private Action<FrameConfiguration> configuration;
        internal FrameConfigurationBuilder()
        {
            configuration ??= descriptor => { };
        }

        public FrameConfigurationBuilder Add(Action<FrameConfiguration> action)
        {
            configuration += action;
            return this;
        }

        public Action<FrameConfiguration> Build() => configuration;
    }
}
