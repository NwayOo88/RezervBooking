using RezervBooking.Application.Abstractions;
using RezervBooking.Domain.Entities;
using StackExchange.Redis;
using System.Collections.Generic;

namespace RezervBooking.Infrastructure.Redis;

public class RedisDistributedLockService : IDistributedLockService
{
    private readonly IConnectionMultiplexer _redis;

    public RedisDistributedLockService(IConnectionMultiplexer redis)
    {
        _redis = redis;
    }

    public async Task<IAsyncDisposable?> TryAcquireAsync(string key, TimeSpan waitTime, TimeSpan expiry, CancellationToken cancellationToken = default)
    {
        var database = _redis.GetDatabase();

        var token = Guid.NewGuid().ToString("N");

        var end = DateTime.UtcNow.Add(waitTime);

        do
        {
            var acquired = await database.StringSetAsync(key, token, expiry, When.NotExists);

            if (acquired)
            {
                return new RedisLockHandle(database, key, token);
            }

            await Task.Delay(50, cancellationToken);

        } while (DateTime.UtcNow < end);

        return null;
    }

    private sealed class RedisLockHandle : IAsyncDisposable
    {
        private readonly IDatabase _database;
        private readonly string _key;
        private readonly string _token;

        public RedisLockHandle(IDatabase database, string key, string token)
        {
            _database = database;
            _key = key;
            _token = token;
        }

        public async ValueTask DisposeAsync()
        {
            const string script = """
                if redis.call("get", KEYS[1]) == ARGV[1] then
                    return redis.call("del", KEYS[1])
                else
                    return 0
                end
                """;

            await _database.ScriptEvaluateAsync(script, new RedisKey[] { _key }, new RedisValue[] { _token });
        }
    }
}

//last seat
//user A + user B
//→ both booking simultaneously

//same user
//two simultaneous requests
//→ double credit spending
//→ overlapping classes