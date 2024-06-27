using Frame.Redis.RedisContexts;

namespace Frame.Redis
{
    public interface IRedisFactory
    {
        IRedisContext GetContext<TRedisContext>() where TRedisContext : RedisContext;
    }

}
