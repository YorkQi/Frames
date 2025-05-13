using Frame.Redis.RedisContexts;

namespace Frame.Redis
{
    public interface IRedisContext
    {
        IRedisDbContext GetDbContext();
    }

}
