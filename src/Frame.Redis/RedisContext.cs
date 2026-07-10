using System;
using System.Threading.Tasks;
using Frame.Redis.RedisDbContexts;

namespace Frame.Redis
{
    public class RedisContext : IDisposable, IAsyncDisposable
    {
        private RedisDbContext? _dbContext;
        private bool disposedValue;

        internal string? RedisConnection { get; set; }

        public IRedisDbContext GetDbContext()
        {
            if (_dbContext is not null)
                return _dbContext;

            _dbContext = new RedisDbContext(
                RedisConnection ?? throw new InvalidOperationException("Redis 连接字符串未设置。"));
            return _dbContext;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _dbContext?.Dispose();
                }

                _dbContext = null;
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        public ValueTask DisposeAsync()
        {
            if (!disposedValue)
            {
                _dbContext?.Dispose();
                _dbContext = null;
                disposedValue = true;
            }
            return default;
        }

        async ValueTask IAsyncDisposable.DisposeAsync()
        {
            await DisposeAsync();
            GC.SuppressFinalize(this);
        }
    }
}
