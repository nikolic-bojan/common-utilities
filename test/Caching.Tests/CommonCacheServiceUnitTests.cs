using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Svea.Eureka.Services.Location.Infrastructure.Services.Cache;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Caching.Tests
{
    [TestClass]
    public class CommonCacheServiceUnitTests
    {
        private const string Test = "test";

        internal class TestClass
        {
            public string MyProperty { get; set; }
        }

        [TestMethod]
        public async Task GetOrCreateStringAsync_EmptyCache()
        {
            const string generatedKey = "key1";

            IMemoryCache memoryCache = new MemoryCache(new MemoryCacheOptions());

            var options = Options.Create(new MemoryDistributedCacheOptions());
            var redisCache = new MemoryDistributedCache(options);

            var logger = Substitute.For<LoggerMock<CommonCachingService>>();

            var cacheService = new CommonCachingService(memoryCache, redisCache, logger);

            var result = await cacheService.GetOrCreateStringAsync(generatedKey, () => Task.FromResult(Test), TimeSpan.FromSeconds(30), TimeSpan.FromMinutes(5));

            Assert.AreEqual(Test, result);

            logger.Received(1).Log(LogLevel.Debug, $"Getting cached value from Distributed cache for key {generatedKey}");
            logger.Received(0).Log(LogLevel.Debug, $"Read cached value from Distributed cache for key {generatedKey}");
            logger.Received(1).Log(LogLevel.Debug, $"Stored in Distributed cache for key {generatedKey}");
        }

        [TestMethod]
        public async Task GetOrCreateStringAsync_MemoryEmpty_RedisContains()
        {
            const string generatedKey = "key1";

            IMemoryCache memoryCache = new MemoryCache(new MemoryCacheOptions());

            var options = Options.Create(new MemoryDistributedCacheOptions());
            var redisCache = new MemoryDistributedCache(options);
            await redisCache.SetStringAsync(generatedKey, Test);

            var logger = Substitute.For<LoggerMock<CommonCachingService>>();

            var cacheService = new CommonCachingService(memoryCache, redisCache, logger);

            var result = await cacheService.GetOrCreateStringAsync(generatedKey, () => Task.FromResult(Test), TimeSpan.FromSeconds(30), TimeSpan.FromMinutes(5));

            Assert.AreEqual(Test, result);

            logger.Received(1).Log(LogLevel.Debug, $"Getting cached value from Distributed cache for key {generatedKey}");
            logger.Received(1).Log(LogLevel.Debug, $"Read cached value from Distributed cache for key {generatedKey}");
            logger.Received(0).Log(LogLevel.Debug, $"Stored in Distributed cache for key {generatedKey}");
        }

        [TestMethod]
        public async Task GetOrCreateStringAsync_MemoryEmpty_RedisContains_CallTwice()
        {
            const string generatedKey = "key1";

            IMemoryCache memoryCache = new MemoryCache(new MemoryCacheOptions());

            var options = Options.Create(new MemoryDistributedCacheOptions());
            var redisCache = new MemoryDistributedCache(options);
            await redisCache.SetStringAsync(generatedKey, Test);

            var logger = Substitute.For<LoggerMock<CommonCachingService>>();

            var cacheService = new CommonCachingService(memoryCache, redisCache, logger);

            var result = await cacheService.GetOrCreateStringAsync(generatedKey, () => Task.FromResult(Test), TimeSpan.FromSeconds(30), TimeSpan.FromMinutes(5));
            Assert.AreEqual(Test, result);

            result = await cacheService.GetOrCreateStringAsync(generatedKey, () => Task.FromResult(Test), TimeSpan.FromSeconds(30), TimeSpan.FromMinutes(5));
            Assert.AreEqual(Test, result);

            logger.Received(1).Log(LogLevel.Debug, $"Getting cached value from Distributed cache for key {generatedKey}");
            logger.Received(1).Log(LogLevel.Debug, $"Read cached value from Distributed cache for key {generatedKey}");
            logger.Received(0).Log(LogLevel.Debug, $"Stored in Distributed cache for key {generatedKey}");
        }

        [TestMethod]
        public async Task GetOrCreateStringAsync_MemoryContains()
        {
            const string generatedKey = "key1";

            IMemoryCache memoryCache = new MemoryCache(new MemoryCacheOptions());
            memoryCache.Set(generatedKey, Test);

            var options = Options.Create(new MemoryDistributedCacheOptions());
            var redisCache = new MemoryDistributedCache(options);

            var logger = Substitute.For<LoggerMock<CommonCachingService>>();

            var cacheService = new CommonCachingService(memoryCache, redisCache, logger);

            var result = await cacheService.GetOrCreateStringAsync(generatedKey, () => Task.FromResult(Test), TimeSpan.FromSeconds(30), TimeSpan.FromMinutes(5));

            Assert.AreEqual(Test, result);

            logger.Received(0).Log(LogLevel.Debug, $"Getting cached value from Distributed cache for key {generatedKey}");
            logger.Received(0).Log(LogLevel.Debug, $"Read cached value from Distributed cache for key {generatedKey}");
            logger.Received(0).Log(LogLevel.Debug, $"Stored in Distributed cache for key {generatedKey}");
        }

        [TestMethod]
        public async Task GetOrCreateAsync_EmptyCache()
        {
            const string generatedKey = "key1";
            var testObject = new TestClass { MyProperty = Test };

            IMemoryCache memoryCache = new MemoryCache(new MemoryCacheOptions());

            var options = Options.Create(new MemoryDistributedCacheOptions());
            var redisCache = new MemoryDistributedCache(options);

            var logger = Substitute.For<LoggerMock<CommonCachingService>>();

            var cacheService = new CommonCachingService(memoryCache, redisCache, logger);

            var result = await cacheService.GetOrCreateAsync(generatedKey, () => Task.FromResult(testObject), TimeSpan.FromSeconds(30), TimeSpan.FromMinutes(5));

            Assert.AreEqual(Test, result.MyProperty);
            
            logger.Received(1).Log(LogLevel.Debug, $"Getting cached value from Distributed cache for key {generatedKey}");
            logger.Received(0).Log(LogLevel.Debug, $"Read cached value from Distributed cache for key {generatedKey}");
            logger.Received(1).Log(LogLevel.Debug, $"Stored in Distributed cache for key {generatedKey}");
        }

        [TestMethod]
        public async Task GetOrCreateAsync_MemoryEmpty_RedisContains()
        {
            const string generatedKey = "key1";
            var testObject = new TestClass { MyProperty = Test };

            IMemoryCache memoryCache = new MemoryCache(new MemoryCacheOptions());

            var options = Options.Create(new MemoryDistributedCacheOptions());
            var redisCache = new MemoryDistributedCache(options);
            await redisCache.SetStringAsync(generatedKey, new JsonConverter<TestClass>().Serialize(testObject));

            var logger = Substitute.For<LoggerMock<CommonCachingService>>();

            var cacheService = new CommonCachingService(memoryCache, redisCache, logger);

            var result = await cacheService.GetOrCreateAsync(generatedKey, () => Task.FromResult(testObject), TimeSpan.FromSeconds(30), TimeSpan.FromMinutes(5));

            Assert.AreEqual(Test, result.MyProperty);

            logger.Received(1).Log(LogLevel.Debug, $"Getting cached value from Distributed cache for key {generatedKey}");
            logger.Received(1).Log(LogLevel.Debug, $"Read cached value from Distributed cache for key {generatedKey}");
            logger.Received(0).Log(LogLevel.Debug, $"Stored in Distributed cache for key {generatedKey}");
        }

        [TestMethod]
        public async Task GetOrCreateAsync_MemoryContains()
        {
            const string generatedKey = "key1";
            var testObject = new TestClass { MyProperty = Test };

            IMemoryCache memoryCache = new MemoryCache(new MemoryCacheOptions());
            memoryCache.Set(generatedKey, testObject);

            var options = Options.Create(new MemoryDistributedCacheOptions());
            var redisCache = new MemoryDistributedCache(options);

            var logger = Substitute.For<LoggerMock<CommonCachingService>>();

            var cacheService = new CommonCachingService(memoryCache, redisCache, logger);

            await cacheService.GetOrCreateAsync(generatedKey, () => Task.FromResult(testObject), TimeSpan.FromSeconds(30), TimeSpan.FromMinutes(5));

            var result = await cacheService.GetOrCreateAsync(generatedKey, () => Task.FromResult(testObject), TimeSpan.FromSeconds(30), TimeSpan.FromMinutes(5));

            Assert.AreEqual(Test, result.MyProperty);

            logger.Received(0).Log(LogLevel.Debug, $"Getting cached value from Distributed cache for key {generatedKey}");
            logger.Received(0).Log(LogLevel.Debug, $"Read cached value from Distributed cache for key {generatedKey}");
            logger.Received(0).Log(LogLevel.Debug, $"Stored in Distributed cache for key {generatedKey}");
        }

        [TestMethod]
        [ExpectedException(typeof(NotSupportedException))]
        public async Task GetOrCreateAsync_NotSupportedType()
        {
            const string generatedKey = "key1";

            IMemoryCache memoryCache = new MemoryCache(new MemoryCacheOptions());

            var options = Options.Create(new MemoryDistributedCacheOptions());
            var redisCache = new MemoryDistributedCache(options);

            var logger = Substitute.For<ILogger<CommonCachingService>>();

            var cacheService = new CommonCachingService(memoryCache, redisCache, logger);

            var result = await cacheService.GetOrCreateAsync(generatedKey, () => Task.FromResult(Test), TimeSpan.FromSeconds(30), TimeSpan.FromMinutes(5));
        }
    }
}
