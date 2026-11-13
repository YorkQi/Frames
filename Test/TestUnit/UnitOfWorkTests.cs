using Frame.Core.Uow;
using Xunit;

namespace TestUnit
{
    public class UnitOfWorkTests
    {
        [Fact]
        public void Begin_SetsCurrentAndSharesItems()
        {
            var manager = new UnitOfWorkManager();

            var outer = manager.Begin();
            var inner = manager.Begin();

            try
            {
                Assert.Same(outer, manager.Current);

                inner.Items["key"] = "value";
                Assert.Equal("value", outer.Items["key"]);
                Assert.Same(outer.Items, inner.Items);
            }
            finally
            {
                inner.Dispose();
                outer.Dispose();
            }
        }

        [Fact]
        public async Task RegisterCompletionHandler_OnNestedUnit_RunsAtRootCommit()
        {
            var manager = new UnitOfWorkManager();
            var log = new List<string>();

            var outer = manager.Begin();
            var inner = manager.Begin();
            inner.RegisterCompletionHandler(_ =>
            {
                log.Add("inner-handler");
                return Task.CompletedTask;
            });

            await inner.CompleteAsync();
            // 嵌套 UOW 只标记完成，不执行处理器
            Assert.Empty(log);

            await outer.CompleteAsync();
            Assert.Equal(new[] { "inner-handler" }, log);
        }

        [Fact]
        public async Task CompleteAsync_RunsHandlersInRegistrationOrder()
        {
            var manager = new UnitOfWorkManager();
            var log = new List<string>();
            var unitOfWork = manager.Begin();

            unitOfWork.RegisterCompletionHandler(_ =>
            {
                log.Add("first");
                return Task.CompletedTask;
            });
            unitOfWork.RegisterCompletionHandler(_ =>
            {
                log.Add("second");
                return Task.CompletedTask;
            });

            await unitOfWork.CompleteAsync();

            Assert.Equal(new[] { "first", "second" }, log);
            Assert.True(unitOfWork.IsCompleted);
        }

        [Fact]
        public async Task RollbackAsync_RunsHandlersInReverseOrder()
        {
            var manager = new UnitOfWorkManager();
            var log = new List<string>();
            var unitOfWork = manager.Begin();

            unitOfWork.RegisterRollbackHandler(() => log.Add("first"));
            unitOfWork.RegisterRollbackHandler(() => log.Add("second"));

            await unitOfWork.RollbackAsync();

            Assert.Equal(new[] { "second", "first" }, log);
            Assert.True(unitOfWork.IsRolledBack);
        }

        [Fact]
        public async Task CompleteAsync_AfterRollback_Throws()
        {
            var manager = new UnitOfWorkManager();
            var unitOfWork = manager.Begin();
            await unitOfWork.RollbackAsync();

            await Assert.ThrowsAsync<InvalidOperationException>(() => unitOfWork.CompleteAsync());
        }

        [Fact]
        public void Dispose_WithoutComplete_RollsBack()
        {
            var manager = new UnitOfWorkManager();
            var log = new List<string>();
            var unitOfWork = manager.Begin();
            unitOfWork.RegisterRollbackHandler(() => log.Add("rollback"));

            unitOfWork.Dispose();

            Assert.Equal(new[] { "rollback" }, log);
        }

        [Fact]
        public async Task CompleteAsync_ClearsCurrent()
        {
            var manager = new UnitOfWorkManager();
            var unitOfWork = manager.Begin();

            await unitOfWork.CompleteAsync();

            Assert.Null(manager.Current);
        }

        [Fact]
        public async Task NestedComplete_ThenOuterRollback_RollsBackEverything()
        {
            var manager = new UnitOfWorkManager();
            var log = new List<string>();

            var outer = manager.Begin();
            var inner = manager.Begin();
            inner.RegisterRollbackHandler(() => log.Add("rollback"));

            await inner.CompleteAsync();
            await outer.RollbackAsync();

            Assert.Equal(new[] { "rollback" }, log);
        }
    }
}
