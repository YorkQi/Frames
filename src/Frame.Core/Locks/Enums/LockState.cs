namespace Frame.Redis.Locks
{
    public enum LockState
    {
        /// <summary>
        /// 锁处于未锁定状态
        /// </summary>
        Unlocked,
        /// <summary>
        /// 锁已成功获取
        /// </summary>
        Acquired,
        /// <summary>
        /// 未达到法定人数（通常用于分布式锁场景）
        /// </summary>
        NoQuorum,
        /// <summary>
        /// 锁状态冲突（如多个客户端同时持有锁）
        /// </summary>
        Conflicted,
        /// <summary>
        /// 锁已过期（超过租约时间）
        /// </summary>
        Expired
    }
}
