using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Svea.Eureka.Services.Location.Infrastructure.Services.Cache;
using System;
using System.Threading.Tasks;

namespace Caching.Examples.Basic
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var cachingService = BuildCachingService();

            string testKey = "TestKey";
            string testKey2 = "TestKey_2";

            Console.Out.WriteLine("Starting with cache calls...");
            Console.Out.WriteLine();

            Console.Out.WriteLine($"Key {testKey}, take 1");
            await cachingService.GetOrCreateStringAsync(testKey, FunctionThatCreatesTaskOfT(), TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(5));
            Console.Out.WriteLine();

            Console.Out.WriteLine($"Key {testKey}, take 2");            
            await cachingService.GetOrCreateStringAsync(testKey, FunctionThatCreatesTaskOfT(), TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(5));
            Console.Out.WriteLine();

            Console.Out.WriteLine($"Key {testKey}, take 3");            
            await cachingService.GetOrCreateStringAsync(testKey, FunctionThatCreatesTaskOfT(), TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(5));
            Console.Out.WriteLine();

            // It is a new Key, it will not be able to get value from cache.
            Console.Out.WriteLine($"Key {testKey2}, take 1");            
            await cachingService.GetOrCreateStringAsync(testKey2, FunctionThatCreatesTaskOfT(), TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(5));
            Console.Out.WriteLine();

            Console.Out.WriteLine("...Cache calls finished.");

            Console.ReadKey();
        }
        
        /// <summary>
        /// Creates a Function that will create Task of T value.
        /// This Function will not be executed if there is a cached vale in either Memory Cache or Distributed Cache.
        /// </summary>
        /// <returns>Function for Task of T value.</returns>
        private static Func<Task<string>> FunctionThatCreatesTaskOfT()
        {            
            return () =>
            {
                // This part will not execute if there is a key match in cache in either Memory or Distributed cache
                Console.Out.WriteLine("We are executing method, not getting from cache.");
                return Task.FromResult("TestValue");
            };
        }

        private static ICommonCachingService BuildCachingService()
        {
            var services = new ServiceCollection();

            services.AddLogging(configure => configure.AddConsole());

            services.AddMemoryCache();
            services.AddDistributedMemoryCache();

            services.AddTransient<ICommonCachingService, CommonCachingService>();

            var provider = services.BuildServiceProvider();

            return provider.GetRequiredService<ICommonCachingService>();

        }
    }
}
