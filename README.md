# Memory+Distributed Caching in .NET Core

We were building yet another adapter toward some 3rd party service. They are caching results on their side for 1 day (7 days for one of the routes), but charging you per call.
OK, if you want to play like that... We can cache also!

> TL;DR; 
> I created a small service class that allows you to cache in both Memory and some Distributed cache and take best of both worlds.
> Visit Git repository https://github.com/nikolic-bojan/common-utilities
> That contains code, unit tests and sample console application.
> Copy/Paste the code and make your flavor.

#Caching in Core

Documentation is really good in .NET Core, so this is no exception. You have 2 options when caching - IMemoryCache and IDistributedCache.

Since our apps run on IIS, even though they are set to Allways Running, our Ops crew set App Pools to restart every 29 hours. So, caching just to memory could work, but we have several servers, we could redeploy, some server restarts...

OK, let's do it then in some Distributed cache. We had Redis setup and that is all good, but it is slower than memory cache. But what if...


# Best of both worlds
Why not caching to both memory and to Redis (or some other Distrubuted cache)? I searched a little bit and stumbled upon a great post by Nick Craver about how they do it in Stack Overflow [How We Do App Caching] (https://nickcraver.com/blog/2019/08/06/stack-overflow-how-we-do-app-caching/) 

OK, we are super-far from that number of requests and needs for caching, but why not make some reusable code for some Caching Service that would have options for both memory and distributed caching?

Here is the logic:
* Check if you have cached item in memory cache (on that server)
* If you have, all good, no need to do anything else
* If you do not - go to Distributed cache and check there
* If it is there, grab it and store it in memory cache also
* If it is not, execute some **"factory"** method that will create the value for cache and store it in distributed and then memory cache

Here is the example of how would you call Caching Service
```csharp
await cachingService.GetOrCreateAsync<TestObject>(
    "key", 
    () => Task.FromResult(new TestObject()), 
    TimeSpan.FromMinutes(1), 
    TimeSpan.FromMinutes(5));
```

Most interesting part is second parameter and that one is a `Func<Task<T>>`. That function will be only executed if Caching Service doesn't find **key** in both memory and distributed cache.
That function will contain some "expensive" part - from code execution or from pricey-3rd-party-call perspective.


# Show us the code!
This is the current state of code on GitHub. I deliberately didn't want to create some NuGet, because I didn't do that yet for our local use also. When I figure out the needs from several projects, I will build something truly reusable, even though it is very reusable now.

{% github https://github.com/nikolic-bojan/common-utilities %}

Interface is fairly simple
```csharp
public interface ICommonCachingService
{
    Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, TimeSpan memoryCacheExpiration, TimeSpan? distributedCacheExpiration = null) where T : class;

    Task<string> GetOrCreateStringAsync(string key, Func<Task<string>> factory, TimeSpan memoryCacheExpiration, TimeSpan? distributedCacheExpiration = null);

    Task<T> GetOrCreateAsync<T>(IConverter<T> converter, string key, Func<Task<T>> factory, TimeSpan memoryCacheExpiration, TimeSpan? distributedCacheExpiration);
}
```
You have 3 methods:
1. Caching objects with JSON serialization.
2. Caching strings without serialization (because it didn't work for me with JSON serialization; I am lazy to figure out why)
3. Caching whatever with custom Converter (implement your serialization)

First two are just calling the third, where the actual implementation is, with a pre-selected Converter.

Parameters for the methods are:
* **key** - A key to cache by, make some method for generating unique one.
* **factory** - I explained it earlier.
* **memoryCacheExpiration** - Time-to-live (TTL) in memory cache. There is no sliding option.
* **distributedCacheExpiration** - TTL in distributed cache.
* **converter** - Simple serialize/deserialize interface as code below shows.

```csharp
public interface IConverter<T>
{
	string Serialize(object obj);

	T Deserialize(string value);
}
```

## Memory cache
```csharp
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
```
MemoryCache already has this handy `GetOrCreateAsync` method that accepts **"factory"** method. I just added setup for expiration and forwarded function call with all the parameters.

## Distributed cache
```csharp
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
		try
		{
			var cacheEntryOptions = new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = calculatedDistributedCacheExpiration };
			var serializedValue = converter.Serialize(item);
			await _distributedCache.SetStringAsync(generatedKey, serializedValue, cacheEntryOptions, CancellationToken.None);
			_logger.LogDebug("Stored in Distributed cache for key {Key}", generatedKey);
		}
		catch (Exception e)
		{
			_logger.LogWarning(e, "Exception storing cached item in Distributed cache.");
		} 
	}
	
	return item;
}
```
This little fellow has a bit more logic:
* Tries to get value from distributed cache and return value if available,
* If it is not there, calls **"factory"** method to create value and then store it in cache.

I also added basic try-catch, so nothing breaks if there is some issue with Redis or some other distributed cache, both for reading and storing.

# Example
You already saw it on the beginning, but let's do it again
```csharp
await cachingService.GetOrCreateAsync<TestObject>(
    "key", 
    () => Task.FromResult(new TestObject()), 
    TimeSpan.FromMinutes(1), 
    TimeSpan.FromMinutes(5));
```
Now you know this tells a story - **try to get from memory/distributed cache a value with a key "key"; if you do not find it, crate it with "factory" method and keep it in memory cache for 1 minute and in distributed cache for 5 minutes**.

You can use it in your Controllers, Core/Domain or your Infrastructure (some of the stuff refers to Onion/Clean/whatever-name architecture). I do not think there is just one answer where to use it. Actually, there is - anywhere!

# Improvements?
* Add some `CacheEntryOptions` class for more fine-grained cache setup.
* Add resilience for calls to distributed cache like retry with circuit breaker.
* Allow more than one Distributed cache (Why dude? It's just more headache!).
* Remove dependency on Logging as I just use it to write unit tests with less hustle.

# Words of caution!
Since one of the toughest things in programming is caching, beware of all potential issues, like:
* Memory pressure if you keep to many objects in cache that are big-ish
* Large object heap, if your objects are >85k
* Invalidation - I am not even thinking about it here, but you might need it.

Why I am not concerned about those in our use-case:
* I am setting low Memory Cache Expiration (e.g. 10 minutes), so hardly I can come to memory pressure situation.
* I know how big are my objects.
* I setup Distributed Cache Expiration also to some time span that I know it will not require me to regret I do not have Invalidate method.

**Know your stuff!**


Comments, questions and suggestions are very much welcome!


Best regards,
Bojan
