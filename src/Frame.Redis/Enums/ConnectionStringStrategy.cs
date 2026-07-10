namespace Frame.Redis.Enums
{
    /// <summary>
    /// Redis 连接字符串选择策略
    /// </summary>
    public enum ConnectionStringStrategy
    {
        /// <summary>随机选择</summary>
        Random,

        /// <summary>轮询选择</summary>
        RoundRobin,
    }
}
