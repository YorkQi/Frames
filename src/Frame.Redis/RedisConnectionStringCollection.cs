using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Frame.Redis.Enums;

namespace Frame.Redis
{
    /// <summary>
    /// Redis 连接字符串集合，支持多策略选取。
    /// </summary>
    public class RedisConnectionStringCollection : IEnumerable<string>
    {
        private readonly List<string> _redisStrs = [];
        private int _roundRobinIndex = -1;

        public RedisConnectionStringCollection(params string[] connections)
        {
            _redisStrs.AddRange(connections);
        }

        public void Add(params string[] connections)
        {
            _redisStrs.AddRange(connections);
        }

        public void AddRange(IEnumerable<string> connections)
        {
            _redisStrs.AddRange(connections);
        }

        /// <summary>
        /// 按指定策略获取一条连接字符串
        /// </summary>
        public string GetConnection(ConnectionStringStrategy strategy)
        {
            if (_redisStrs.Count == 0)
                throw new InvalidOperationException("没有可用的 Redis 连接字符串");

            return strategy switch
            {
                ConnectionStringStrategy.RoundRobin => GetRoundRobin(),
                _ => GetRandom(),
            };
        }

        /// <summary>
        /// 轮询获取一条连接字符串（默认策略）
        /// </summary>
        public string GetConnection()
        {
            return GetConnection(ConnectionStringStrategy.RoundRobin);
        }

        private static readonly Random _random = new();

        private string GetRandom()
        {
            lock (_random)
            {
                return _redisStrs[_random.Next(_redisStrs.Count)];
            }
        }

        private string GetRoundRobin()
        {
            var index = Interlocked.Increment(ref _roundRobinIndex) % _redisStrs.Count;
            return _redisStrs[index];
        }

        public IEnumerable<string> Get()
        {
            return _redisStrs;
        }

        public IEnumerator<string> GetEnumerator()
        {
            return _redisStrs.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _redisStrs.GetEnumerator();
        }
    }
}
