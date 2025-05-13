using System;

namespace Frame.Core.FrameModules
{
    public static class FrameModuleConfigurationBuilder
    {
        public static FrameConfigurationBuilder Create(this FrameConfigurationBuilder builder)
        {
            builder.Add(descriptor =>
            {
                descriptor.UseModule();
                descriptor.UseApplication();
            });
            return builder;
        }
    }
}
