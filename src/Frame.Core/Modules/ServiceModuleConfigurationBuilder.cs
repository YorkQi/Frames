namespace Frame.Core
{
    public static class ServiceModuleConfigurationBuilder
    {
        public static ServiceConfigurationBuilder Create(this ServiceConfigurationBuilder builder)
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
