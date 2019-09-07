using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Svea.Eureka.Services.Location.Infrastructure.Services.Cache
{
    /// <inheritdoc />
    public class CommonCachingService : ICommonCachingService
    {
        IMemoryCache _memoryCache;
        IDistributedCache _distributedCache;
        private readonly ILogger<CommonCachingService> _logger;

        public CommonCachingService(IMemoryCache memoryCache, IDistributedCache distributedCache, ILogger<CommonCachingService> logger)
        {
            _memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
            _distributedCache = distributedCache ?? throw new ArgumentNullException(nameof(distributedCache));
            _logger = logger;
        }

        public async Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, TimeSpan memoryCacheExpiration, TimeSpan? distributedCacheExpiration) where T : class
        {
            if (typeof(T) == typeof(string))
            {
                throw new NotSupportedException("This method does not support 'string' type. Please use GetOrCreateStringAsync method instead.");
            }
            return await GetOrCreateAsync(new JsonConverter<T>(), key, factory, memoryCacheExpiration, distributedCacheExpiration);
        }

        public async Task<string> GetOrCreateStringAsync(string key, Func<Task<string>> factory, TimeSpan memoryCacheExpiration, TimeSpan? distributedCacheExpiration = null)
        {
            return await GetOrCreateAsync(new StringConverter(), key, factory, memoryCacheExpiration, distributedCacheExpiration);
        }

        public async Task<T> GetOrCreateAsync<T>(IConverter<T> converter, string key, Func<Task<T>> factory, TimeSpan memoryCacheExpiration, TimeSpan? distributedCacheExpiration)
        {
            var local = await _memoryCache.GetOrCreateAsync(key, entry =>
            {
                TimeSpan calculatedDistributedCacheExpiration = distributedCacheExpiration ?? memoryCacheExpiration;

                entry.AbsoluteExpiration = DateTime.UtcNow.Add(memoryCacheExpiration);
                return GetFromDistributedCache(converter, key, factory, calculatedDistributedCacheExpiration);
            });

            return local;
        }

        private async Task<T> GetFromDistributedCache<T>(IConverter<T> converter, string generatedKey, Func<Task<T>> factory, TimeSpan calculatedDistributedCacheExpiration)
        {
            _logger.LogDebug("Getting cached value from Distributed cache for key {Key}", generatedKey);
            try
            {
                var cachedItem = await _distributedCache.GetStringAsync(generatedKey);
                if (cachedItem != null)
                {
                    _logger.LogDebug("Read cached value from Distributed cache for key {Key}", generatedKey);
                    var value = converter.Deserialize(cachedItem);
                    return value;
                }
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, "Exception getting cached item from Distributed cache.");
            }
            
            var item = await factory.Invoke();
            if (item != null)
            {
                var cacheEntryOptions = new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = calculatedDistributedCacheExpiration };
                var serializedValue = converter.Serialize(item);
                await _distributedCache.SetStringAsync(generatedKey, serializedValue, cacheEntryOptions, CancellationToken.None);
                _logger.LogDebug("Stored in Distributed cache for key {Key}", generatedKey);
            }
            
            return item;
        }
    }
}