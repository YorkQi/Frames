using System;
using System.Threading.Tasks;
using Frame.Redis.RedisDbContexts;
using StackExchange.Redis;

namespace Frame.Redis
{
    /// <summary>
    /// Redis 请求上下文。
    /// </summary>
    public class RedisContext : IDisposable, IAsyncDisposable
    {
        private RedisDbContext? _dbContext;
        private bool disposedValue;

        /// <summary>
        /// Redis 连接复用器（单例）。由框架在初始化时注入。
        /// </summary>
        internal ConnectionMultiplexer? Multiplexer { get; set; }

        /// <summary>
        /// 获取 Redis 数据库操作接口。
        /// 每次调用返回同一个内部 <see cref="RedisDbContext"/> 实例。
        /// </summary>
        /// <exception cref="InvalidOperationException">ConnectionMultiplexer 未设置时抛出</exception>
        public IRedisDbContext GetDbContext()
        {
            if (_dbContext is not null)
                return _dbContext;

            var mux = Multiplexer
                ?? throw new InvalidOperationException("Redis ConnectionMultiplexer 未设置。请确保已调用 UseRedisDatabase 注册 Redis 服务。");

            _dbContext = new RedisDbContext(mux);
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
