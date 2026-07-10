using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Frame.Redis
{
    /// <summary>
    /// Redis 数据库上下文接口，覆盖常用 Redis 数据类型与操作。
    /// 同步 / 异步方法一一对应，实现层基于 StackExchange.Redis。
    /// </summary>
    public interface IRedisDbContext
    {
        // ============================================================
        // 生命周期
        // ============================================================

        /// <summary>
        /// 选择要使用的 Redis 数据库
        /// </summary>
        /// <param name="databaseNumber">数据库编号（0-15）</param>
        void SelectDatabase(int databaseNumber);

        // ============================================================
        // Key
        // ============================================================

        /// <summary>检查键是否存在</summary>
        bool KeyExists(string key);

        /// <summary>检查键是否存在</summary>
        Task<bool> KeyExistsAsync(string key);

        /// <summary>删除指定键</summary>
        bool DeleteKey(string key);

        /// <summary>删除指定键</summary>
        Task<bool> DeleteKeyAsync(string key);

        /// <summary>设置键的过期时间</summary>
        bool KeyExpire(string key, TimeSpan expiry);

        /// <summary>设置键的过期时间</summary>
        Task<bool> KeyExpireAsync(string key, TimeSpan expiry);

        /// <summary>获取键的剩余生存时间，永不过期返回 null</summary>
        TimeSpan? KeyTimeToLive(string key);

        /// <summary>获取键的剩余生存时间，永不过期返回 null</summary>
        Task<TimeSpan?> KeyTimeToLiveAsync(string key);

        /// <summary>移除键的过期时间，使其永久有效</summary>
        bool KeyPersist(string key);

        /// <summary>移除键的过期时间，使其永久有效</summary>
        Task<bool> KeyPersistAsync(string key);

        /// <summary>重命名键，newKey 已存在时覆盖</summary>
        bool KeyRename(string key, string newKey);

        /// <summary>重命名键，newKey 已存在时覆盖</summary>
        Task<bool> KeyRenameAsync(string key, string newKey);

        // ============================================================
        // String
        // ============================================================

        /// <summary>设置字符串键值对</summary>
        void Set(string key, string value);

        /// <summary>设置字符串键值对</summary>
        Task SetAsync(string key, string value);

        /// <summary>设置字符串键值对并指定过期时间</summary>
        void Set(string key, string value, TimeSpan expiry);

        /// <summary>设置字符串键值对并指定过期时间</summary>
        Task SetAsync(string key, string value, TimeSpan expiry);

        /// <summary>仅当键不存在时设置，返回是否成功（SETNX）</summary>
        bool SetNx(string key, string value);

        /// <summary>仅当键不存在时设置，返回是否成功（SETNX）</summary>
        Task<bool> SetNxAsync(string key, string value);

        /// <summary>仅当键不存在时设置，并指定过期时间</summary>
        bool SetNx(string key, string value, TimeSpan expiry);

        /// <summary>仅当键不存在时设置，并指定过期时间</summary>
        Task<bool> SetNxAsync(string key, string value, TimeSpan expiry);

        /// <summary>获取键的字符串值，不存在返回 null</summary>
        string? Get(string key);

        /// <summary>获取键的字符串值，不存在返回 null</summary>
        Task<string?> GetAsync(string key);

        /// <summary>设置新值并返回旧值</summary>
        string? GetSet(string key, string value);

        /// <summary>设置新值并返回旧值</summary>
        Task<string?> GetSetAsync(string key, string value);

        /// <summary>自增（默认为 1）</summary>
        long Increment(string key, long value = 1);

        /// <summary>自增（默认为 1）</summary>
        Task<long> IncrementAsync(string key, long value = 1);

        /// <summary>自减（默认为 1）</summary>
        long Decrement(string key, long value = 1);

        /// <summary>自减（默认为 1）</summary>
        Task<long> DecrementAsync(string key, long value = 1);

        /// <summary>浮点自增</summary>
        double IncrementFloat(string key, double value);

        /// <summary>浮点自增</summary>
        Task<double> IncrementFloatAsync(string key, double value);

        /// <summary>获取字符串值的长度</summary>
        long StringLength(string key);

        /// <summary>获取字符串值的长度</summary>
        Task<long> StringLengthAsync(string key);

        /// <summary>追加字符串到键值末尾，返回追加后的长度</summary>
        long StringAppend(string key, string value);

        /// <summary>追加字符串到键值末尾，返回追加后的长度</summary>
        Task<long> StringAppendAsync(string key, string value);

        /// <summary>批量获取多个键的值</summary>
        string?[] MGet(params string[] keys);

        /// <summary>批量获取多个键的值</summary>
        Task<string?[]> MGetAsync(params string[] keys);

        /// <summary>批量设置键值对</summary>
        void MSet(Dictionary<string, string> keyValues);

        /// <summary>批量设置键值对</summary>
        Task MSetAsync(Dictionary<string, string> keyValues);

        // ============================================================
        // List
        // ============================================================

        /// <summary>向列表左端插入一个元素，返回插入后列表长度</summary>
        long ListLeftPush(string key, string value);

        /// <summary>向列表左端插入一个元素，返回插入后列表长度</summary>
        Task<long> ListLeftPushAsync(string key, string value);

        /// <summary>向列表右端插入一个元素，返回插入后列表长度</summary>
        long ListRightPush(string key, string value);

        /// <summary>向列表右端插入一个元素，返回插入后列表长度</summary>
        Task<long> ListRightPushAsync(string key, string value);

        /// <summary>从列表左端弹出元素，列表为空返回 null</summary>
        string? ListLeftPop(string key);

        /// <summary>从列表左端弹出元素，列表为空返回 null</summary>
        Task<string?> ListLeftPopAsync(string key);

        /// <summary>从列表右端弹出元素，列表为空返回 null</summary>
        string? ListRightPop(string key);

        /// <summary>从列表右端弹出元素，列表为空返回 null</summary>
        Task<string?> ListRightPopAsync(string key);

        /// <summary>获取列表长度</summary>
        long ListLength(string key);

        /// <summary>获取列表长度</summary>
        Task<long> ListLengthAsync(string key);

        /// <summary>获取列表指定范围的元素（start/stop 支持负数倒数）</summary>
        string?[] ListRange(string key, long start, long stop);

        /// <summary>获取列表指定范围的元素（start/stop 支持负数倒数）</summary>
        Task<string?[]> ListRangeAsync(string key, long start, long stop);

        /// <summary>移除列表中等于 value 的元素，count &gt; 0 从左移除，&lt; 0 从右移除，= 0 全部移除</summary>
        long ListRemove(string key, string value, long count = 0);

        /// <summary>移除列表中等于 value 的元素</summary>
        Task<long> ListRemoveAsync(string key, string value, long count = 0);

        /// <summary>按索引获取列表元素</summary>
        string? ListGetByIndex(string key, long index);

        /// <summary>按索引获取列表元素</summary>
        Task<string?> ListGetByIndexAsync(string key, long index);

        /// <summary>按索引设置列表元素</summary>
        void ListSetByIndex(string key, long index, string value);

        /// <summary>按索引设置列表元素</summary>
        Task ListSetByIndexAsync(string key, long index, string value);

        /// <summary>裁剪列表，仅保留 [start, stop] 范围内的元素</summary>
        void ListTrim(string key, long start, long stop);

        /// <summary>裁剪列表，仅保留 [start, stop] 范围内的元素</summary>
        Task ListTrimAsync(string key, long start, long stop);

        // ============================================================
        // Set
        // ============================================================

        /// <summary>向集合添加成员，已存在返回 false</summary>
        bool SetAdd(string key, string member);

        /// <summary>向集合添加成员，已存在返回 false</summary>
        Task<bool> SetAddAsync(string key, string member);

        /// <summary>从集合移除成员</summary>
        bool SetRemove(string key, string member);

        /// <summary>从集合移除成员</summary>
        Task<bool> SetRemoveAsync(string key, string member);

        /// <summary>检查成员是否在集合中</summary>
        bool SetContains(string key, string member);

        /// <summary>检查成员是否在集合中</summary>
        Task<bool> SetContainsAsync(string key, string member);

        /// <summary>获取集合成员数量</summary>
        long SetLength(string key);

        /// <summary>获取集合成员数量</summary>
        Task<long> SetLengthAsync(string key);

        /// <summary>获取集合所有成员</summary>
        string[] SetMembers(string key);

        /// <summary>获取集合所有成员</summary>
        Task<string[]> SetMembersAsync(string key);

        /// <summary>随机获取集合中的一个成员</summary>
        string? SetRandomMember(string key);

        /// <summary>随机获取集合中的一个成员</summary>
        Task<string?> SetRandomMemberAsync(string key);

        /// <summary>随机获取集合中的多个成员</summary>
        string?[] SetRandomMembers(string key, long count);

        /// <summary>随机获取集合中的多个成员</summary>
        Task<string?[]> SetRandomMembersAsync(string key, long count);

        /// <summary>返回两个集合的并集</summary>
        string[] SetUnion(string first, string second);

        /// <summary>返回两个集合的并集</summary>
        Task<string[]> SetUnionAsync(string first, string second);

        /// <summary>返回两个集合的交集</summary>
        string[] SetIntersect(string first, string second);

        /// <summary>返回两个集合的交集</summary>
        Task<string[]> SetIntersectAsync(string first, string second);

        /// <summary>返回两个集合的差集（first 中有而 second 中没有的）</summary>
        string[] SetDifference(string first, string second);

        /// <summary>返回两个集合的差集</summary>
        Task<string[]> SetDifferenceAsync(string first, string second);

        // ============================================================
        // Hash
        // ============================================================

        /// <summary>设置哈希表字段值，字段新增返回 true，更新返回 false</summary>
        bool HashSet(string key, string field, string value);

        /// <summary>设置哈希表字段值，字段新增返回 true，更新返回 false</summary>
        Task<bool> HashSetAsync(string key, string field, string value);

        /// <summary>批量设置哈希表字段值</summary>
        void HashSet(string key, Dictionary<string, string> fields);

        /// <summary>批量设置哈希表字段值</summary>
        Task HashSetAsync(string key, Dictionary<string, string> fields);

        /// <summary>获取哈希表指定字段的值，不存在返回 null</summary>
        string? HashGet(string key, string field);

        /// <summary>获取哈希表指定字段的值，不存在返回 null</summary>
        Task<string?> HashGetAsync(string key, string field);

        /// <summary>批量获取哈希表多个字段的值</summary>
        string?[] HashGet(string key, params string[] fields);

        /// <summary>批量获取哈希表多个字段的值</summary>
        Task<string?[]> HashGetAsync(string key, params string[] fields);

        /// <summary>获取哈希表所有字段和值</summary>
        Dictionary<string, string> HashGetAll(string key);

        /// <summary>获取哈希表所有字段和值</summary>
        Task<Dictionary<string, string>> HashGetAllAsync(string key);

        /// <summary>检查哈希表中字段是否存在</summary>
        bool HashExists(string key, string field);

        /// <summary>检查哈希表中字段是否存在</summary>
        Task<bool> HashExistsAsync(string key, string field);

        /// <summary>删除哈希表指定字段，存在并删除返回 true</summary>
        bool HashDelete(string key, string field);

        /// <summary>删除哈希表指定字段，存在并删除返回 true</summary>
        Task<bool> HashDeleteAsync(string key, string field);

        /// <summary>批量删除哈希表多个字段，返回实际删除的字段数</summary>
        long HashDelete(string key, params string[] fields);

        /// <summary>批量删除哈希表多个字段，返回实际删除的字段数</summary>
        Task<long> HashDeleteAsync(string key, params string[] fields);

        /// <summary>获取哈希表所有字段名</summary>
        string[] HashKeys(string key);

        /// <summary>获取哈希表所有字段名</summary>
        Task<string[]> HashKeysAsync(string key);

        /// <summary>获取哈希表所有字段值</summary>
        string[] HashValues(string key);

        /// <summary>获取哈希表所有字段值</summary>
        Task<string[]> HashValuesAsync(string key);

        /// <summary>获取哈希表字段数量</summary>
        long HashLength(string key);

        /// <summary>获取哈希表字段数量</summary>
        Task<long> HashLengthAsync(string key);

        /// <summary>哈希表字段整型自增</summary>
        long HashIncrement(string key, string field, long value = 1);

        /// <summary>哈希表字段整型自增</summary>
        Task<long> HashIncrementAsync(string key, string field, long value = 1);

        /// <summary>哈希表字段浮点自增</summary>
        double HashIncrementFloat(string key, string field, double value);

        /// <summary>哈希表字段浮点自增</summary>
        Task<double> HashIncrementFloatAsync(string key, string field, double value);

        // ============================================================
        // SortedSet
        // ============================================================

        /// <summary>向有序集合添加成员，新增返回 true，更新分数返回 false</summary>
        bool SortedSetAdd(string key, string member, double score);

        /// <summary>向有序集合添加成员，新增返回 true，更新分数返回 false</summary>
        Task<bool> SortedSetAddAsync(string key, string member, double score);

        /// <summary>从有序集合移除成员</summary>
        bool SortedSetRemove(string key, string member);

        /// <summary>从有序集合移除成员</summary>
        Task<bool> SortedSetRemoveAsync(string key, string member);

        /// <summary>移除有序集合中分数在 [start, stop] 范围内的成员，返回移除数量</summary>
        long SortedSetRemoveRangeByScore(string key, double start, double stop);

        /// <summary>移除有序集合中分数在 [start, stop] 范围内的成员，返回移除数量</summary>
        Task<long> SortedSetRemoveRangeByScoreAsync(string key, double start, double stop);

        /// <summary>移除有序集合中排名在 [start, stop] 范围内的成员，返回移除数量</summary>
        long SortedSetRemoveRangeByRank(string key, long start, long stop);

        /// <summary>移除有序集合中排名在 [start, stop] 范围内的成员，返回移除数量</summary>
        Task<long> SortedSetRemoveRangeByRankAsync(string key, long start, long stop);

        /// <summary>获取有序集合成员的分数，不存在返回 null</summary>
        double? SortedSetScore(string key, string member);

        /// <summary>获取有序集合成员的分数，不存在返回 null</summary>
        Task<double?> SortedSetScoreAsync(string key, string member);

        /// <summary>增加有序集合成员的分数，返回新分数</summary>
        double SortedSetIncrement(string key, string member, double value);

        /// <summary>增加有序集合成员的分数，返回新分数</summary>
        Task<double> SortedSetIncrementAsync(string key, string member, double value);

        /// <summary>减少有序集合成员的分数，返回新分数</summary>
        double SortedSetDecrement(string key, string member, double value);

        /// <summary>减少有序集合成员的分数，返回新分数</summary>
        Task<double> SortedSetDecrementAsync(string key, string member, double value);

        /// <summary>获取有序集合成员数量</summary>
        long SortedSetLength(string key);

        /// <summary>获取有序集合成员数量</summary>
        Task<long> SortedSetLengthAsync(string key);

        /// <summary>获取有序集合中分数在 [min, max] 范围内的成员数量</summary>
        long SortedSetLengthByScore(string key, double min, double max);

        /// <summary>获取有序集合中分数在 [min, max] 范围内的成员数量</summary>
        Task<long> SortedSetLengthByScoreAsync(string key, double min, double max);

        /// <summary>按分数升序获取 [start, stop] 范围内的成员</summary>
        string[] SortedSetRangeByScore(string key, double start, double stop);

        /// <summary>按分数升序获取 [start, stop] 范围内的成员</summary>
        Task<string[]> SortedSetRangeByScoreAsync(string key, double start, double stop);

        /// <summary>按排名升序获取 [start, stop] 范围内的成员（0 为第一名）</summary>
        string?[] SortedSetRangeByRank(string key, long start, long stop);

        /// <summary>按排名升序获取 [start, stop] 范围内的成员（0 为第一名）</summary>
        Task<string?[]> SortedSetRangeByRankAsync(string key, long start, long stop);

        /// <summary>按分数升序获取 [start, stop] 范围内的成员及分数</summary>
        Dictionary<string, double> SortedSetRangeByScoreWithScores(string key, double start, double stop);

        /// <summary>按分数升序获取 [start, stop] 范围内的成员及分数</summary>
        Task<Dictionary<string, double>> SortedSetRangeByScoreWithScoresAsync(string key, double start, double stop);

        // ============================================================
        // Pub / Sub
        // ============================================================

        /// <summary>向频道发布消息，返回收到消息的订阅者数量</summary>
        long Publish(string channel, string message);

        /// <summary>向频道发布消息，返回收到消息的订阅者数量</summary>
        Task<long> PublishAsync(string channel, string message);
    }
}
