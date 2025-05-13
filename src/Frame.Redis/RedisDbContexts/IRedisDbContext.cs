using System.Collections.Generic;
using System;

namespace Frame.Redis.RedisContexts
{
    public interface IRedisDbContext
    {
        /// <summary>
        /// 初始化 Redis 连接
        /// </summary>
        /// <param name="redisConnection">Redis 连接字符串</param>
        void Initialize(string redisConnection);

        /// <summary>
        /// 选择要使用的 Redis 数据库
        /// </summary>
        /// <param name="databaseNumber">数据库编号（0-15）</param>
        void SelectDatabase(int databaseNumber);

        /// <summary>
        /// 设置字符串类型的键值对
        /// </summary>
        /// <param name="key">键名</param>
        /// <param name="value">字符串值</param>
        void Set(string key, string value);

        /// <summary>
        /// 获取指定键的字符串值
        /// </summary>
        /// <param name="key">键名</param>
        /// <returns>对应的字符串值，如果键不存在则返回 null</returns>
        string Get(string key);

        /// <summary>
        /// 检查指定键是否存在
        /// </summary>
        /// <param name="key">要检查的键名</param>
        /// <returns>如果键存在返回 true，否则返回 false</returns>
        bool KeyExists(string key);

        /// <summary>
        /// 删除指定的键
        /// </summary>
        /// <param name="key">要删除的键名</param>
        /// <returns>如果键被成功删除返回 true，否则返回 false</returns>
        bool DeleteKey(string key);

        /// <summary>
        /// 向列表左端插入元素
        /// </summary>
        /// <param name="key">列表键名</param>
        /// <param name="value">要插入的值</param>
        /// <returns>插入后列表的长度</returns>
        long ListLeftPush(string key, string value);

        /// <summary>
        /// 从列表右端弹出元素
        /// </summary>
        /// <param name="key">列表键名</param>
        /// <returns>弹出的元素值，如果列表为空返回 null</returns>
        string ListRightPop(string key);

        /// <summary>
        /// 向集合添加成员
        /// </summary>
        /// <param name="key">集合键名</param>
        /// <param name="member">要添加的成员</param>
        /// <returns>如果成员已存在返回 false，新增成功返回 true</returns>
        bool SetAdd(string key, string member);

        /// <summary>
        /// 获取集合所有成员
        /// </summary>
        /// <param name="key">集合键名</param>
        /// <returns>包含所有成员的枚举集合</returns>
        IEnumerable<string> SetMembers(string key);

        /// <summary>
        /// 设置哈希表字段的值
        /// </summary>
        /// <param name="key">哈希表键名</param>
        /// <param name="field">字段名</param>
        /// <param name="value">字段值</param>
        /// <returns>如果字段是新增的返回 true，如果是更新返回 false</returns>
        bool HashSet(string key, string field, string value);

        /// <summary>
        /// 获取哈希表指定字段的值
        /// </summary>
        /// <param name="key">哈希表键名</param>
        /// <param name="field">字段名</param>
        /// <returns>字段值，如果字段不存在返回 null</returns>
        string HashGet(string key, string field);

        /// <summary>
        /// 获取哈希表所有字段名
        /// </summary>
        /// <param name="key">哈希表键名</param>
        /// <returns>包含所有字段名的枚举集合</returns>
        IEnumerable<string> HashKeys(string key);  // 注意：这里原代码缺少分号

        /// <summary>
        /// 获取哈希表所有字段值
        /// </summary>
        /// <param name="key">哈希表键名</param>
        /// <returns>包含所有字段值的枚举集合</returns>
        IEnumerable<string> HashValues(string key);

        /// <summary>
        /// 删除哈希表指定字段
        /// </summary>
        /// <param name="key">哈希表键名</param>
        /// <param name="field">要删除的字段名</param>
        /// <returns>如果字段存在并被删除返回 true，否则返回 false</returns>
        bool HashDelete(string key, string field);

        /// <summary>
        /// 向有序集合添加成员
        /// </summary>
        /// <param name="key">有序集合键名</param>
        /// <param name="member">成员值</param>
        /// <param name="score">成员分数</param>
        /// <returns>如果成员是新添加的返回 true，如果是更新分数返回 false</returns>
        bool SortedSetAdd(string key, string member, double score);

        /// <summary>
        /// 根据分数范围获取有序集合成员
        /// </summary>
        /// <param name="key">有序集合键名</param>
        /// <param name="start">起始分数</param>
        /// <param name="stop">结束分数</param>
        /// <returns>分数范围内的成员枚举集合</returns>
        IEnumerable<string> SortedSetRangeByScore(string key, double start, double stop);
    }
}
