using System;

namespace Frame.Core.FrameModules
{
    public class FrameModuleConfigurationBuilder
    {
        private Action<FrameConfiguration> configuration;
        internal FrameModuleConfigurationBuilder()
        {
            configuration ??= descriptor => { };
        }

        public FrameModuleConfigurationBuilder AddModule()
        {
            configuration += descriptor =>
            {
                descriptor.UseModule();
                descriptor.UseApplication();
            };
            return this;
        }

        public Action<FrameConfiguration> Build() => configuration;
    }
}
