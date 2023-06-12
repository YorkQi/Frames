namespace Frame.Redis.Locks
{
    public enum RedisLockState
    {
        /// <summary>
        /// 
        /// </summary>
        Unlocked,
        /// <summary>
        /// 
        /// </summary>
        Acquired,
        /// <summary>
        /// 
        /// </summary>
        NoQuorum,
        /// <summary>
        /// 
        /// </summary>
        Conflicted,
        /// <summary>
        /// 
        /// </summary>
        Expired
    }
}
