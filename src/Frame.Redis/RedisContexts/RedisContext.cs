using Frame.Redis.Locks;
using Frame.Redis.RedisContexts;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Frame.Redis
{
    public partial class RedisContext : IRedisContext
    {
        private ConnectionMultiplexer? _redis;
        private IDatabase? _db;

        private readonly RedisConnection connection;
        public RedisContext(RedisConnection connection)
        {
            this.connection = connection;
        }

        internal void InitializeContext()
        {
            _redis = ConnectionMultiplexer.Connect(connection.First());
            _db = _redis.GetDatabase();
        }

        public void SelectDatabase(int databaseNumber)
        {
            if (_redis is null) throw new ArgumentNullException(nameof(_redis));
            _db = _redis.GetDatabase(databaseNumber);
        }


        public void Set(string key, string value)
        {
            if (_db is null) throw new ArgumentNullException(nameof(_db));
            _db.StringSet(key, value);
        }

        public string Get(string key)
        {
            if (_db is null) throw new ArgumentNullException(nameof(_db));
            return _db.StringGet(key).ToString();
        }

        public bool KeyExists(string key)
        {
            if (_db is null) throw new ArgumentNullException(nameof(_db));
            return _db.KeyExists(key);
        }

        public bool DeleteKey(string key)
        {
            if (_db is null) throw new ArgumentNullException(nameof(_db));
            return _db.KeyDelete(key);
        }

        public long ListLeftPush(string key, string value)
        {
            if (_db is null) throw new ArgumentNullException(nameof(_db));
            return _db.ListLeftPush(key, value);
        }

        public string ListRightPop(string key)
        {
            if (_db is null) throw new ArgumentNullException(nameof(_db));
            return _db.ListRightPop(key).ToString();
        }

        public bool SetAdd(string key, string member)
        {
            if (_db is null) throw new ArgumentNullException(nameof(_db));
            return _db.SetAdd(key, member);
        }

        public IEnumerable<string> SetMembers(string key)
        {
            if (_db is null) throw new ArgumentNullException(nameof(_db));
            return _db.SetMembers(key).ToStringArray().Select(_ => _ ?? string.Empty);
        }

        public bool HashSet(string key, string field, string value)
        {
            if (_db is null) throw new ArgumentNullException(nameof(_db));
            return _db.HashSet(key, field, value);
        }

        public string HashGet(string key, string field)
        {
            if (_db is null) throw new ArgumentNullException(nameof(_db));
            return _db.HashGet(key, field).ToString();
        }

        public IEnumerable<string> HashKeys(string key)
        {
            if (_db is null) throw new ArgumentNullException(nameof(_db));
            return _db.HashKeys(key).ToStringArray().Select(_ => _ ?? string.Empty);
        }
        public IEnumerable<string> HashValues(string key)
        {
            if (_db is null) throw new ArgumentNullException(nameof(_db));
            return _db.HashValues(key).ToStringArray().Select(_ => _ ?? string.Empty);
        }
        public bool HashDelete(string key, string field)
        {
            if (_db is null) throw new ArgumentNullException(nameof(_db));
            return _db.HashDelete(key, field);
        }

        public bool SortedSetAdd(string key, string member, double score)
        {
            if (_db is null) throw new ArgumentNullException(nameof(_db));
            return _db.SortedSetAdd(key, member, score);
        }

        public IEnumerable<string> SortedSetRangeByScore(string key, double start, double stop)
        {
            if (_db is null) throw new ArgumentNullException(nameof(_db));
            return _db.SortedSetRangeByScore(key, start, stop).ToStringArray().Select(_ => _ ?? string.Empty);
        }

        public long Publish(string channel, string message)
        {
            if (_db is null) throw new ArgumentNullException(nameof(_db));
            return _db.Publish(channel, message);
        }

        public void Subscribe(string channel, Action<RedisChannel, RedisValue> handler)
        {
            if (_redis is null) throw new ArgumentNullException(nameof(_db));
            ISubscriber subscriber = _redis.GetSubscriber();
            subscriber.Subscribe(channel, (redisChannel, value) => handler(redisChannel, value));
        }
        public void CloseConnection()
        {
            _redis?.Close();
        }
    }
}
