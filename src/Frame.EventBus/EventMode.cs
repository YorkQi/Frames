namespace Frame.EventBus
{
    /// <summary>
    /// 事件执行方式枚举
    /// </summary>
    public enum EventMode
    {
        /// <summary>
        /// 本地缓存队列
        /// </summary>
        LocalCache = 1,
        /// <summary>
        /// Redis队列
        /// </summary>
        Redis = 2,
        /// <summary>
        /// RabbitMQ队列
        /// </summary>
        RabbitMQ = 3
    }
}
