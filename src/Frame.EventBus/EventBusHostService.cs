using Microsoft.Extensions.Hosting;
using System;
using System.Reflection;
using System.Threading;

using System.Threading.Tasks;

namespace Frame.EventBus
{
    public class EventBusHostService : IHostedService, IDisposable
    {
        private readonly ManualResetEvent signal = new(false);
        private readonly CancellationTokenSource cancel = new();
        private readonly Task processingTask;

        private readonly int interval = 100;//间隔100毫秒

        private readonly IEventBus eventBus;

        public EventBusHostService(IEventBus eventBus)
        {
            processingTask = new Task(async () => await Exec(cancel.Token));
            this.eventBus = eventBus;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            processingTask.Start();
            signal.Set();
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            signal?.Reset();
            signal?.Dispose();
            cancel?.Cancel();
            cancel?.Dispose();
            processingTask?.Dispose();
            return Task.CompletedTask;
        }

        private bool disposed = false;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    signal?.Reset();
                    signal?.Dispose();
                    cancel?.Cancel();
                    cancel?.Dispose();
                    processingTask?.Dispose();
                }

                disposed = true;
            }
        }

        /// <summary>
        /// 执行事件订阅
        /// </summary>
        /// <returns></returns>
        /// <exception cref="ApplicationException"></exception>
        private async Task Exec(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                MethodInfo? method = eventBus.GetType().GetMethod("Exec", BindingFlags.Instance | BindingFlags.NonPublic);
                if (method != null)
                {
                    _ = method.Invoke(eventBus, new object[] { cancel.Token });
                }
                await Task.Delay(interval, token);
            }
        }
    }
}
