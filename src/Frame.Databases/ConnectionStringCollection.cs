using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using Frame.Core.Utils;
using Frame.Databases.Enums;

namespace Frame.Databases
{
    /// <summary>
    /// 多数据库连接字符串集合，支持批量添加、遍历及多策略选取。
    /// </summary>
    public class ConnectionStringCollection : IEnumerable<string>
    {
        [NotNull]
        private readonly List<string> _connectionStrings;
        private int _roundRobinIndex = -1;

        public ConnectionStringCollection()
        {
            _connectionStrings = [];
        }

        public ConnectionStringCollection([NotNull] params string[] connections)
        {
            Check.NotNull(connections, nameof(connections));
            _connectionStrings = [.. connections];
        }

        public void Add([NotNull] params string[] connectionStrings)
        {
            Check.NotNull(connectionStrings, nameof(connectionStrings));
            _connectionStrings.AddRange(connectionStrings);
        }

        public void AddRange([NotNull] IEnumerable<string> connectionStrings)
        {
            Check.NotNull(connectionStrings, nameof(connectionStrings));
            _connectionStrings.AddRange(connectionStrings);
        }

        public string[] ToArray()
        {
            return [.. _connectionStrings];
        }

        /// <summary>
        /// 按指定策略获取一条连接字符串
        /// </summary>
        public string GetConnection(ConnectionStringStrategy strategy)
        {
            if (_connectionStrings.Count == 0)
                throw new InvalidOperationException("没有可用的连接字符串");

            return strategy switch
            {
                ConnectionStringStrategy.RoundRobin => GetRoundRobin(),
                _ => GetRandom(),
            };
        }

        private string GetRandom()
        {
            return _connectionStrings[Random.Shared.Next(_connectionStrings.Count)];
        }

        private string GetRoundRobin()
        {
            var index = Interlocked.Increment(ref _roundRobinIndex) % _connectionStrings.Count;
            return _connectionStrings[index];
        }

        public IEnumerator<string> GetEnumerator()
        {
            return _connectionStrings.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
