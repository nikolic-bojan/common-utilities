using System;
using System.Threading.Tasks;

namespace Svea.Eureka.Services.Location.Infrastructure.Services.Cache
{
    /// <summary>
    /// Common Caching Service
    /// </summary>
    public interface ICommonCachingService
    {
        /// <summary>
        /// Gets and/or Creates a cached item.
        /// ONLY FOR INSTANCES OF A CLASS, EXCLUDING STRING!
        /// It works with Json serialization.
        /// </summary>
        /// <typeparam name="T">Type of cached item value.</typeparam>
        /// <param name="key">Key for cached item.</param>
        /// <param name="factory">Factory method to create cached item value, if not in cache.</param>
        /// <param name="memoryCacheExpiration">Expiration of in-memory cached item. Recommended to be smaller TimeSpan to reduce memory pressure.</param>
        /// <param name="distributedCacheExpiration">Expiration of cached item on Redis. Can be some bigger TimeSpan.</param>
        /// <returns>Cached value for sent key.</returns>
        Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, TimeSpan memoryCacheExpiration, TimeSpan? distributedCacheExpiration = null) where T : class;

        /// <summary>
        /// Gets and/or Creates a cached string item.
        /// </summary>        
        /// <param name="key">Key for cached item.</param>
        /// <param name="factory">Factory method to create cached item value, if not in cache.</param>
        /// <param name="memoryCacheExpiration">Expiration of in-memory cached item. Recommended to be smaller TimeSpan to reduce memory pressure.</param>
        /// <param name="distributedCacheExpiration">Expiration of cached item on Redis. Can be some bigger TimeSpan.</param>
        /// <returns>Cached value for sent key.</returns>
        Task<string> GetOrCreateStringAsync(string key, Func<Task<string>> factory, TimeSpan memoryCacheExpiration, TimeSpan? distributedCacheExpiration = null);

        /// <summary>
        /// Gets and/or Creates a cahed item with custom converter.
        /// </summary>
        /// <typeparam name="T">Type of cached item value.</typeparam>
        /// <param name="converter">Implementation of IConverter interface that serialize/deserialize cached item value.</param>
        /// <param name="key">Key for cached item.</param>
        /// <param name="factory">Factory method to create cached item value, if not in cache.</param>
        /// <param name="memoryCacheExpiration">Expiration of in-memory cached item. Recommended to be smaller TimeSpan to reduce memory pressure.</param>
        /// <param name="distributedCacheExpiration">Expiration of cached item on Redis. Can be some bigger TimeSpan.</param>
        /// <returns>Cached value for sent key.</returns>
        Task<T> GetOrCreateAsync<T>(IConverter<T> converter, string key, Func<Task<T>> factory, TimeSpan memoryCacheExpiration, TimeSpan? distributedCacheExpiration);
    }
}