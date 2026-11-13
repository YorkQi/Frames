using Frame.Core.Uow;
using Frame.Core.Uow.Interception;
using Xunit;

namespace TestUnit
{
    public class UnitOfWorkProxyTests
    {
        public interface IOrderService
        {
            Task CreateAsync();
            Task<int> CountAsync();
            Task FailAsync();
            int SyncCount();
        }

        [UnitOfWork]
        private sealed class OrderService : IOrderService
        {
            private readonly List<string> _log;
            private readonly IUnitOfWorkManager _manager;

            public OrderService(List<string> log, IUnitOfWorkManager manager)
            {
                _log = log;
                _manager = manager;
            }

            public Task CreateAsync()
            {
                _log.Add("invoke");
                _manager.Current!.RegisterCompletionHandler(_ =>
                {
                    _log.Add("commit");
                    return Task.CompletedTask;
                });
                _manager.Current!.RegisterRollbackHandler(() => _log.Add("rollback"));
                return Task.CompletedTask;
            }

            public Task<int> CountAsync()
            {
                _log.Add("invoke");
                _manager.Current!.RegisterCompletionHandler(_ =>
                {
                    _log.Add("commit");
                    return Task.CompletedTask;
                });
                return Task.FromResult(42);
            }

            public Task FailAsync()
            {
                _log.Add("invoke");
                _manager.Current!.RegisterRollbackHandler(() => _log.Add("rollback"));
                throw new InvalidOperationException("boom");
            }

            public int SyncCount()
            {
                _log.Add("invoke");
                _manager.Current!.RegisterCompletionHandler(_ =>
                {
                    _log.Add("commit");
                    return Task.CompletedTask;
                });
                return 7;
            }
        }

        public interface IInterfaceMarkedService
        {
            [UnitOfWork]
            Task RunAsync();
        }

        public class InterfaceMarkedService : IInterfaceMarkedService
        {
            private readonly List<string> _log;
            private readonly IUnitOfWorkManager _manager;

            public InterfaceMarkedService(List<string> log, IUnitOfWorkManager manager)
            {
                _log = log;
                _manager = manager;
            }

            public Task RunAsync()
            {
                _log.Add("invoke");
                _manager.Current!.RegisterCompletionHandler(_ =>
                {
                    _log.Add("commit");
                    return Task.CompletedTask;
                });
                return Task.CompletedTask;
            }
        }

        public interface IImplementationMarkedService
        {
            Task RunAsync();
        }

        public class ImplementationMarkedService : IImplementationMarkedService
        {
            private readonly List<string> _log;
            private readonly IUnitOfWorkManager _manager;

            public ImplementationMarkedService(List<string> log, IUnitOfWorkManager manager)
            {
                _log = log;
                _manager = manager;
            }

            [UnitOfWork]
            public Task RunAsync()
            {
                _log.Add("invoke");
                _manager.Current!.RegisterCompletionHandler(_ =>
                {
                    _log.Add("commit");
                    return Task.CompletedTask;
                });
                return Task.CompletedTask;
            }
        }

        [Fact]
        public async Task AttributeOnInterfaceMethod_EnablesUnitOfWork()
        {
            var manager = new UnitOfWorkManager();
            var log = new List<string>();
            var service = UnitOfWorkProxy.Create<IInterfaceMarkedService>(
                new InterfaceMarkedService(log, manager), manager);

            await service.RunAsync();

            Assert.Equal(new[] { "invoke", "commit" }, log);
        }

        [Fact]
        public async Task AttributeOnImplementationMethod_EnablesUnitOfWork()
        {
            var manager = new UnitOfWorkManager();
            var log = new List<string>();
            var service = UnitOfWorkProxy.Create<IImplementationMarkedService>(
                new ImplementationMarkedService(log, manager), manager);

            await service.RunAsync();

            Assert.Equal(new[] { "invoke", "commit" }, log);
        }

        [Fact]
        public async Task CreateAsync_CommitsAfterTargetReturns()
        {
            var manager = new UnitOfWorkManager();
            var log = new List<string>();
            var service = UnitOfWorkProxy.Create<IOrderService>(new OrderService(log, manager), manager);

            await service.CreateAsync();

            Assert.Equal(new[] { "invoke", "commit" }, log);
            Assert.Null(manager.Current);
        }

        [Fact]
        public async Task CountAsync_CommitsAndReturnsValue()
        {
            var manager = new UnitOfWorkManager();
            var log = new List<string>();
            var service = UnitOfWorkProxy.Create<IOrderService>(new OrderService(log, manager), manager);

            var result = await service.CountAsync();

            Assert.Equal(42, result);
            Assert.Equal(new[] { "invoke", "commit" }, log);
            Assert.Null(manager.Current);
        }

        [Fact]
        public async Task FailAsync_RollsBackAndRethrows()
        {
            var manager = new UnitOfWorkManager();
            var log = new List<string>();
            var service = UnitOfWorkProxy.Create<IOrderService>(new OrderService(log, manager), manager);

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => service.FailAsync());

            Assert.Equal("boom", exception.Message);
            Assert.Equal(new[] { "invoke", "rollback" }, log);
            Assert.Null(manager.Current);
        }

        [Fact]
        public void SyncCount_CommitsSynchronously()
        {
            var manager = new UnitOfWorkManager();
            var log = new List<string>();
            var service = UnitOfWorkProxy.Create<IOrderService>(new OrderService(log, manager), manager);

            var result = service.SyncCount();

            Assert.Equal(7, result);
            Assert.Equal(new[] { "invoke", "commit" }, log);
            Assert.Null(manager.Current);
        }

        [Fact]
        public void Create_ThrowsOnNullTarget()
        {
            var manager = new UnitOfWorkManager();

            Assert.Throws<ArgumentNullException>(() => UnitOfWorkProxy.Create<IOrderService>(null!, manager));
        }

        [Fact]
        public void NonGenericCreate_ReturnsProxyImplementingInterface()
        {
            var manager = new UnitOfWorkManager();
            var log = new List<string>();

            var proxy = UnitOfWorkProxy.Create(typeof(IOrderService), new OrderService(log, manager), manager);

            Assert.IsAssignableFrom<IOrderService>(proxy);
        }
    }
}
