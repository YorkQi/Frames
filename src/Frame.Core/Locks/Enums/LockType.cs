namespace Frame.Redis.Locks
{
    public enum LockType
    {
        /// <summary>
        /// 本地锁
        /// </summary>
        Local,
        /// <summary>
        /// redis锁
        /// </summary>
        Redis
    }
}
