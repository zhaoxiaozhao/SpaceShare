using System.Collections.Concurrent;
using FriendlySeat.Application.Common;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace FriendlySeat.Infrastructure.Services;

public class RedisOptions
{
    public string ConnectionString { get; set; } = "localhost:6379";
    public bool Enabled { get; set; } = true;
}

public class DistributedLockService : IDistributedLock
{
    private readonly IConnectionMultiplexer? _redis;
    private readonly bool _enabled;
    private readonly TimeSpan _defaultTimeout = TimeSpan.FromSeconds(15);
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _localLocks = new();

    public DistributedLockService(IConnectionMultiplexer? redis, IOptions<RedisOptions> options)
    {
        _redis = redis;
        _enabled = options.Value.Enabled && redis is not null;
    }

    public async Task<IDistributedLockHandle?> AcquireAsync(string key, TimeSpan timeout, CancellationToken ct = default)
    {
        if (!_enabled || _redis is null)
        {
            var semaphore = _localLocks.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));
            try
            {
                await semaphore.WaitAsync(ct);
            }
            catch (OperationCanceledException)
            {
                return null;
            }
            return new LocalLockHandle(key, semaphore, releaseLocal: true);
        }

        var db = _redis.GetDatabase();
        var token = Guid.NewGuid().ToString("N");
        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            var ok = await db.StringSetAsync($"lock:{key}", token, TimeSpan.FromSeconds(30), When.NotExists);
            if (ok)
            {
                return new RedisLockHandle(key, token, db);
            }
            await Task.Delay(50, ct);
        }
        return null;
    }

    private sealed class RedisLockHandle : IDistributedLockHandle
    {
        private readonly string _key;
        private readonly string _token;
        private readonly IDatabase _db;
        private int _disposed;

        public RedisLockHandle(string key, string token, IDatabase db)
        {
            _key = key;
            _token = token;
            _db = db;
        }

        public bool IsAcquired => true;

        public async ValueTask DisposeAsync()
        {
            if (Interlocked.Exchange(ref _disposed, 1) == 0)
            {
                // 用 Lua 脚本保证只有持有者才能释放
                var script = """
                    if redis.call('get', KEYS[1]) == ARGV[1] then
                        return redis.call('del', KEYS[1])
                    else
                        return 0
                    end
                    """;
                await _db.ScriptEvaluateAsync(script, new RedisKey[] { $"lock:{_key}" }, new RedisValue[] { _token });
            }
        }
    }

    private sealed class LocalLockHandle : IDistributedLockHandle
    {
        private readonly string _key;
        private readonly SemaphoreSlim _semaphore;
        private readonly bool _releaseLocal;
        private int _disposed;

        public LocalLockHandle(string key, SemaphoreSlim semaphore, bool releaseLocal)
        {
            _key = key;
            _semaphore = semaphore;
            _releaseLocal = releaseLocal;
        }

        public bool IsAcquired => true;

        public ValueTask DisposeAsync()
        {
            if (Interlocked.Exchange(ref _disposed, 1) == 0)
            {
                _semaphore.Release();
            }
            return ValueTask.CompletedTask;
        }
    }
}
