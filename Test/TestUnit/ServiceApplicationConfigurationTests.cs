using Frame.Core;
using Frame.Core.Application;
using Frame.Core.Uow;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace TestUnit
{
    public interface IMarkedAppService : IApplicationService
    {
        [UnitOfWork]
        Task RunAsync();
    }

    public class MarkedAppService : IMarkedAppService
    {
        public Task RunAsync() => Task.CompletedTask;
    }

    public interface IPlainAppService : IApplicationService
    {
        Task RunAsync();
    }

    public class PlainAppService : IPlainAppService
    {
        public Task RunAsync() => Task.CompletedTask;
    }

    public class ServiceApplicationConfigurationTests
    {
        [Fact]
        public void UseApplication_WrapsServiceWithUnitOfWorkProxy_WhenInterfaceMethodIsMarked()
        {
            var context = new ServiceConfigurationContext(new[] { typeof(MarkedAppService).Assembly });

            context.UseApplication();

            var descriptor = Assert.Single(
                context.GetServiceDescriptors(), d => d.ServiceType == typeof(IMarkedAppService));
            Assert.NotNull(descriptor.ImplementationFactory);
        }

        [Fact]
        public void UseApplication_RegistersPlainServiceDirectly()
        {
            var context = new ServiceConfigurationContext(new[] { typeof(PlainAppService).Assembly });

            context.UseApplication();

            var descriptor = Assert.Single(
                context.GetServiceDescriptors(), d => d.ServiceType == typeof(IPlainAppService));
            Assert.Null(descriptor.ImplementationFactory);
            Assert.Equal(typeof(PlainAppService), descriptor.ImplementationType);
            Assert.Equal(ServiceLifetime.Scoped, descriptor.Lifetime);
        }
    }
}
