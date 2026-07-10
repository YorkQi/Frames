using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using StackExchange.Redis;

namespace Frame.Redis.RedisDbContexts
{
    internal sealed class RedisDbContext : IRedisDbContext, IDisposable
    {
        private readonly ConnectionMultiplexer _redis;
        private IDatabase _db;

        public RedisDbContext(string redisConnection)
        {
            _redis = ConnectionMultiplexer.Connect(redisConnection);
            _db = _redis.GetDatabase();
        }

        // ============================================================
        // 生命周期
        // ============================================================

        public void SelectDatabase(int databaseNumber)
        {
            _db = _redis.GetDatabase(databaseNumber);
        }

        public void Dispose()
        {
            _redis.Close();
        }

        // ============================================================
        // Key
        // ============================================================

        public bool KeyExists(string key)
        {
            return _db.KeyExists(key);
        }

        public async Task<bool> KeyExistsAsync(string key)
        {
            return await _db.KeyExistsAsync(key);
        }

        public bool DeleteKey(string key)
        {
            return _db.KeyDelete(key);
        }

        public async Task<bool> DeleteKeyAsync(string key)
        {
            return await _db.KeyDeleteAsync(key);
        }

        public bool KeyExpire(string key, TimeSpan expiry)
        {
            return _db.KeyExpire(key, expiry);
        }

        public async Task<bool> KeyExpireAsync(string key, TimeSpan expiry)
        {
            return await _db.KeyExpireAsync(key, expiry);
        }

        public TimeSpan? KeyTimeToLive(string key)
        {
            return _db.KeyTimeToLive(key);
        }

        public async Task<TimeSpan?> KeyTimeToLiveAsync(string key)
        {
            return await _db.KeyTimeToLiveAsync(key);
        }

        public bool KeyPersist(string key)
        {
            return _db.KeyPersist(key);
        }

        public async Task<bool> KeyPersistAsync(string key)
        {
            return await _db.KeyPersistAsync(key);
        }

        public bool KeyRename(string key, string newKey)
        {
            return _db.KeyRename(key, newKey);
        }

        public async Task<bool> KeyRenameAsync(string key, string newKey)
        {
            return await _db.KeyRenameAsync(key, newKey);
        }

        // ============================================================
        // String
        // ============================================================

        public void Set(string key, string value)
        {
            
_db.StringSet(key, value);
        }

        public async Task SetAsync(string key, string value)
        {
            
await _db.StringSetAsync(key, value);
        }

        public void Set(string key, string value, TimeSpan expiry)
        {
            
_db.StringSet(key, value, expiry);
        }

        public async Task SetAsync(string key, string value, TimeSpan expiry)
        {
            
await _db.StringSetAsync(key, value, expiry);
        }

        public bool SetNx(string key, string value)
        {
            return _db.StringSet(key, value, null, When.NotExists);
        }

        public async Task<bool> SetNxAsync(string key, string value)
        {
            return await _db.StringSetAsync(key, value, null, When.NotExists);
        }

        public bool SetNx(string key, string value, TimeSpan expiry)
        {
            return _db.StringSet(key, value, expiry, When.NotExists);
        }

        public async Task<bool> SetNxAsync(string key, string value, TimeSpan expiry)
        {
            return await _db.StringSetAsync(key, value, expiry, When.NotExists);
        }

        public string? Get(string key)
        {
            
var value = _db.StringGet(key);
            return value.HasValue ? value.ToString() : null;
        }

        public async Task<string?> GetAsync(string key)
        {
            
var value = await _db.StringGetAsync(key);
            return value.HasValue ? value.ToString() : null;
        }

        public string? GetSet(string key, string value)
        {
            
var old = _db.StringGetSet(key, value);
            return old.HasValue ? old.ToString() : null;
        }

        public async Task<string?> GetSetAsync(string key, string value)
        {
            
var old = await _db.StringGetSetAsync(key, value);
            return old.HasValue ? old.ToString() : null;
        }

        public long Increment(string key, long value = 1)
        {
            return _db.StringIncrement(key, value);
        }

        public async Task<long> IncrementAsync(string key, long value = 1)
        {
            return await _db.StringIncrementAsync(key, value);
        }

        public long Decrement(string key, long value = 1)
        {
            return _db.StringDecrement(key, value);
        }

        public async Task<long> DecrementAsync(string key, long value = 1)
        {
            return await _db.StringDecrementAsync(key, value);
        }

        public double IncrementFloat(string key, double value)
        {
            return _db.StringIncrement(key, value);
        }

        public async Task<double> IncrementFloatAsync(string key, double value)
        {
            return await _db.StringIncrementAsync(key, value);
        }

        public long StringLength(string key)
        {
            return _db.StringLength(key);
        }

        public async Task<long> StringLengthAsync(string key)
        {
            return await _db.StringLengthAsync(key);
        }

        public long StringAppend(string key, string value)
        {
            return _db.StringAppend(key, value);
        }

        public async Task<long> StringAppendAsync(string key, string value)
        {
            return await _db.StringAppendAsync(key, value);
        }

        public string?[] MGet(params string[] keys)
        {
            
var redisKeys = Array.ConvertAll(keys, k => (RedisKey)k);
            var values = _db.StringGet(redisKeys);
            return ToNullableArray(values);
        }

        public async Task<string?[]> MGetAsync(params string[] keys)
        {
            
var redisKeys = Array.ConvertAll(keys, k => (RedisKey)k);
            var values = await _db.StringGetAsync(redisKeys);
            return ToNullableArray(values);
        }

        public void MSet(Dictionary<string, string> keyValues)
        {
            
var pairs = keyValues.Select(kv =>
                new KeyValuePair<RedisKey, RedisValue>(kv.Key, kv.Value)).ToArray();
            _db.StringSet(pairs);
        }

        public async Task MSetAsync(Dictionary<string, string> keyValues)
        {
            
var pairs = keyValues.Select(kv =>
                new KeyValuePair<RedisKey, RedisValue>(kv.Key, kv.Value)).ToArray();
            await _db.StringSetAsync(pairs);
        }

        // ============================================================
        // List
        // ============================================================

        public long ListLeftPush(string key, string value)
        {
            return _db.ListLeftPush(key, value);
        }

        public async Task<long> ListLeftPushAsync(string key, string value)
        {
            return await _db.ListLeftPushAsync(key, value);
        }

        public long ListRightPush(string key, string value)
        {
            return _db.ListRightPush(key, value);
        }

        public async Task<long> ListRightPushAsync(string key, string value)
        {
            return await _db.ListRightPushAsync(key, value);
        }

        public string? ListLeftPop(string key)
        {
            
var value = _db.ListLeftPop(key);
            return value.HasValue ? value.ToString() : null;
        }

        public async Task<string?> ListLeftPopAsync(string key)
        {
            
var value = await _db.ListLeftPopAsync(key);
            return value.HasValue ? value.ToString() : null;
        }

        public string? ListRightPop(string key)
        {
            
var value = _db.ListRightPop(key);
            return value.HasValue ? value.ToString() : null;
        }

        public async Task<string?> ListRightPopAsync(string key)
        {
            
var value = await _db.ListRightPopAsync(key);
            return value.HasValue ? value.ToString() : null;
        }

        public long ListLength(string key)
        {
            return _db.ListLength(key);
        }

        public async Task<long> ListLengthAsync(string key)
        {
            return await _db.ListLengthAsync(key);
        }

        public string?[] ListRange(string key, long start, long stop)
        {
            
var values = _db.ListRange(key, start, stop);
            return ToNullableArray(values);
        }

        public async Task<string?[]> ListRangeAsync(string key, long start, long stop)
        {
            
var values = await _db.ListRangeAsync(key, start, stop);
            return ToNullableArray(values);
        }

        public long ListRemove(string key, string value, long count = 0)
        {
            return _db.ListRemove(key, value, count);
        }

        public async Task<long> ListRemoveAsync(string key, string value, long count = 0)
        {
            return await _db.ListRemoveAsync(key, value, count);
        }

        public string? ListGetByIndex(string key, long index)
        {
            
var value = _db.ListGetByIndex(key, index);
            return value.HasValue ? value.ToString() : null;
        }

        public async Task<string?> ListGetByIndexAsync(string key, long index)
        {
            
var value = await _db.ListGetByIndexAsync(key, index);
            return value.HasValue ? value.ToString() : null;
        }

        public void ListSetByIndex(string key, long index, string value)
        {
            
_db.ListSetByIndex(key, index, value);
        }

        public async Task ListSetByIndexAsync(string key, long index, string value)
        {
            
await _db.ListSetByIndexAsync(key, index, value);
        }

        public void ListTrim(string key, long start, long stop)
        {
            
_db.ListTrim(key, start, stop);
        }

        public async Task ListTrimAsync(string key, long start, long stop)
        {
            
await _db.ListTrimAsync(key, start, stop);
        }

        // ============================================================
        // Set
        // ============================================================

        public bool SetAdd(string key, string member)
        {
            return _db.SetAdd(key, member);
        }

        public async Task<bool> SetAddAsync(string key, string member)
        {
            return await _db.SetAddAsync(key, member);
        }

        public bool SetRemove(string key, string member)
        {
            return _db.SetRemove(key, member);
        }

        public async Task<bool> SetRemoveAsync(string key, string member)
        {
            return await _db.SetRemoveAsync(key, member);
        }

        public bool SetContains(string key, string member)
        {
            return _db.SetContains(key, member);
        }

        public async Task<bool> SetContainsAsync(string key, string member)
        {
            return await _db.SetContainsAsync(key, member);
        }

        public long SetLength(string key)
        {
            return _db.SetLength(key);
        }

        public async Task<long> SetLengthAsync(string key)
        {
            return await _db.SetLengthAsync(key);
        }

        public string[] SetMembers(string key)
        {
            return ToStringArrayInternal(_db.SetMembers(key));
        }

        public async Task<string[]> SetMembersAsync(string key)
        {
            
var values = await _db.SetMembersAsync(key);
            return ToStringArrayInternal(values);
        }

        public string? SetRandomMember(string key)
        {
            
var value = _db.SetRandomMember(key);
            return value.HasValue ? value.ToString() : null;
        }

        public async Task<string?> SetRandomMemberAsync(string key)
        {
            
var value = await _db.SetRandomMemberAsync(key);
            return value.HasValue ? value.ToString() : null;
        }

        public string?[] SetRandomMembers(string key, long count)
        {
            return ToNullableArray(_db.SetRandomMembers(key, count));
        }

        public async Task<string?[]> SetRandomMembersAsync(string key, long count)
        {
            
var values = await _db.SetRandomMembersAsync(key, count);
            return ToNullableArray(values);
        }

        public string[] SetUnion(string first, string second)
        {
            return ToStringArrayInternal(_db.SetCombine(SetOperation.Union, first, second));
        }

        public async Task<string[]> SetUnionAsync(string first, string second)
        {
            
var values = await _db.SetCombineAsync(SetOperation.Union, first, second);
            return ToStringArrayInternal(values);
        }

        public string[] SetIntersect(string first, string second)
        {
            return ToStringArrayInternal(_db.SetCombine(SetOperation.Intersect, first, second));
        }

        public async Task<string[]> SetIntersectAsync(string first, string second)
        {
            
var values = await _db.SetCombineAsync(SetOperation.Intersect, first, second);
            return ToStringArrayInternal(values);
        }

        public string[] SetDifference(string first, string second)
        {
            return ToStringArrayInternal(_db.SetCombine(SetOperation.Difference, first, second));
        }

        public async Task<string[]> SetDifferenceAsync(string first, string second)
        {
            
var values = await _db.SetCombineAsync(SetOperation.Difference, first, second);
            return ToStringArrayInternal(values);
        }

        // ============================================================
        // Hash
        // ============================================================

        public bool HashSet(string key, string field, string value)
        {
            return _db.HashSet(key, field, value);
        }

        public async Task<bool> HashSetAsync(string key, string field, string value)
        {
            return await _db.HashSetAsync(key, field, value);
        }

        public void HashSet(string key, Dictionary<string, string> fields)
        {
            
var entries = fields.Select(kv =>
                new HashEntry(kv.Key, kv.Value)).ToArray();
            _db.HashSet(key, entries);
        }

        public async Task HashSetAsync(string key, Dictionary<string, string> fields)
        {
            
var entries = fields.Select(kv =>
                new HashEntry(kv.Key, kv.Value)).ToArray();
            await _db.HashSetAsync(key, entries);
        }

        public string? HashGet(string key, string field)
        {
            
var value = _db.HashGet(key, field);
            return value.HasValue ? value.ToString() : null;
        }

        public async Task<string?> HashGetAsync(string key, string field)
        {
            
var value = await _db.HashGetAsync(key, field);
            return value.HasValue ? value.ToString() : null;
        }

        public string?[] HashGet(string key, params string[] fields)
        {
            
var redisFields = Array.ConvertAll(fields, f => (RedisValue)f);
            var values = _db.HashGet(key, redisFields);
            return ToNullableArray(values);
        }

        public async Task<string?[]> HashGetAsync(string key, params string[] fields)
        {
            
var redisFields = Array.ConvertAll(fields, f => (RedisValue)f);
            var values = await _db.HashGetAsync(key, redisFields);
            return ToNullableArray(values);
        }

        public Dictionary<string, string> HashGetAll(string key)
        {
            
var entries = _db.HashGetAll(key);
            var result = new Dictionary<string, string>(entries.Length);
            foreach (var entry in entries)
                result[entry.Name.ToString()] = entry.Value.ToString();
            return result;
        }

        public async Task<Dictionary<string, string>> HashGetAllAsync(string key)
        {
            
var entries = await _db.HashGetAllAsync(key);
            var result = new Dictionary<string, string>(entries.Length);
            foreach (var entry in entries)
                result[entry.Name.ToString()] = entry.Value.ToString();
            return result;
        }

        public bool HashExists(string key, string field)
        {
            return _db.HashExists(key, field);
        }

        public async Task<bool> HashExistsAsync(string key, string field)
        {
            return await _db.HashExistsAsync(key, field);
        }

        public bool HashDelete(string key, string field)
        {
            return _db.HashDelete(key, field);
        }

        public async Task<bool> HashDeleteAsync(string key, string field)
        {
            return await _db.HashDeleteAsync(key, field);
        }

        public long HashDelete(string key, params string[] fields)
        {
            
var redisFields = Array.ConvertAll(fields, f => (RedisValue)f);
            return _db.HashDelete(key, redisFields);
        }

        public async Task<long> HashDeleteAsync(string key, params string[] fields)
        {
            
var redisFields = Array.ConvertAll(fields, f => (RedisValue)f);
            return await _db.HashDeleteAsync(key, redisFields);
        }

        public string[] HashKeys(string key)
        {
            return ToStringArrayInternal(_db.HashKeys(key));
        }

        public async Task<string[]> HashKeysAsync(string key)
        {
            
var values = await _db.HashKeysAsync(key);
            return ToStringArrayInternal(values);
        }

        public string[] HashValues(string key)
        {
            return ToStringArrayInternal(_db.HashValues(key));
        }

        public async Task<string[]> HashValuesAsync(string key)
        {
            
var values = await _db.HashValuesAsync(key);
            return ToStringArrayInternal(values);
        }

        public long HashLength(string key)
        {
            return _db.HashLength(key);
        }

        public async Task<long> HashLengthAsync(string key)
        {
            return await _db.HashLengthAsync(key);
        }

        public long HashIncrement(string key, string field, long value = 1)
        {
            return _db.HashIncrement(key, field, value);
        }

        public async Task<long> HashIncrementAsync(string key, string field, long value = 1)
        {
            return await _db.HashIncrementAsync(key, field, value);
        }

        public double HashIncrementFloat(string key, string field, double value)
        {
            return _db.HashIncrement(key, field, value);
        }

        public async Task<double> HashIncrementFloatAsync(string key, string field, double value)
        {
            return await _db.HashIncrementAsync(key, field, value);
        }

        // ============================================================
        // SortedSet
        // ============================================================

        public bool SortedSetAdd(string key, string member, double score)
        {
            return _db.SortedSetAdd(key, member, score);
        }

        public async Task<bool> SortedSetAddAsync(string key, string member, double score)
        {
            return await _db.SortedSetAddAsync(key, member, score);
        }

        public bool SortedSetRemove(string key, string member)
        {
            return _db.SortedSetRemove(key, member);
        }

        public async Task<bool> SortedSetRemoveAsync(string key, string member)
        {
            return await _db.SortedSetRemoveAsync(key, member);
        }

        public long SortedSetRemoveRangeByScore(string key, double start, double stop)
        {
            return _db.SortedSetRemoveRangeByScore(key, start, stop);
        }

        public async Task<long> SortedSetRemoveRangeByScoreAsync(string key, double start, double stop)
        {
            return await _db.SortedSetRemoveRangeByScoreAsync(key, start, stop);
        }

        public long SortedSetRemoveRangeByRank(string key, long start, long stop)
        {
            return _db.SortedSetRemoveRangeByRank(key, start, stop);
        }

        public async Task<long> SortedSetRemoveRangeByRankAsync(string key, long start, long stop)
        {
            return await _db.SortedSetRemoveRangeByRankAsync(key, start, stop);
        }

        public double? SortedSetScore(string key, string member)
        {
            return _db.SortedSetScore(key, member);
        }

        public async Task<double?> SortedSetScoreAsync(string key, string member)
        {
            return await _db.SortedSetScoreAsync(key, member);
        }

        public double SortedSetIncrement(string key, string member, double value)
        {
            return _db.SortedSetIncrement(key, member, value);
        }

        public async Task<double> SortedSetIncrementAsync(string key, string member, double value)
        {
            return await _db.SortedSetIncrementAsync(key, member, value);
        }

        public double SortedSetDecrement(string key, string member, double value)
        {
            return _db.SortedSetDecrement(key, member, value);
        }

        public async Task<double> SortedSetDecrementAsync(string key, string member, double value)
        {
            return await _db.SortedSetDecrementAsync(key, member, value);
        }

        public long SortedSetLength(string key)
        {
            return _db.SortedSetLength(key);
        }

        public async Task<long> SortedSetLengthAsync(string key)
        {
            return await _db.SortedSetLengthAsync(key);
        }

        public long SortedSetLengthByScore(string key, double min, double max)
        {
            return _db.SortedSetLength(key, min, max);
        }

        public async Task<long> SortedSetLengthByScoreAsync(string key, double min, double max)
        {
            return await _db.SortedSetLengthAsync(key, min, max);
        }

        public string[] SortedSetRangeByScore(string key, double start, double stop)
        {
            return ToStringArrayInternal(_db.SortedSetRangeByScore(key, start, stop));
        }

        public async Task<string[]> SortedSetRangeByScoreAsync(string key, double start, double stop)
        {
            
var values = await _db.SortedSetRangeByScoreAsync(key, start, stop);
            return ToStringArrayInternal(values);
        }

        public string?[] SortedSetRangeByRank(string key, long start, long stop)
        {
            return ToNullableArray(_db.SortedSetRangeByRank(key, start, stop));
        }

        public async Task<string?[]> SortedSetRangeByRankAsync(string key, long start, long stop)
        {
            
var values = await _db.SortedSetRangeByRankAsync(key, start, stop);
            return ToNullableArray(values);
        }

        public Dictionary<string, double> SortedSetRangeByScoreWithScores(string key, double start, double stop)
        {
            
var entries = _db.SortedSetRangeByScoreWithScores(key, start, stop);
            var result = new Dictionary<string, double>(entries.Length);
            foreach (var entry in entries)
                result[entry.Element.ToString()] = entry.Score;
            return result;
        }

        public async Task<Dictionary<string, double>> SortedSetRangeByScoreWithScoresAsync(string key, double start, double stop)
        {
            
var entries = await _db.SortedSetRangeByScoreWithScoresAsync(key, start, stop);
            var result = new Dictionary<string, double>(entries.Length);
            foreach (var entry in entries)
                result[entry.Element.ToString()] = entry.Score;
            return result;
        }

        // ============================================================
        // Pub / Sub
        // ============================================================

        public long Publish(string channel, string message)
        {
            return _redis.GetSubscriber().Publish(RedisChannel.Literal(channel), message);
        }

        public async Task<long> PublishAsync(string channel, string message)
        {
            return await _redis.GetSubscriber().PublishAsync(RedisChannel.Literal(channel), message);
        }

        // ============================================================
        // Helpers
        // ============================================================

        private static string[] ToStringArrayInternal(RedisValue[] values)
        {
            return Array.ConvertAll(values, v => v.ToString());
        }

        private static string?[] ToNullableArray(RedisValue[] values)
        {
            return Array.ConvertAll(values, v => v.HasValue ? (string?)v.ToString() : null);
        }
    }
}
