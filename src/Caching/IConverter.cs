using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Caching.Tests")]
namespace Svea.Eureka.Services.Location.Infrastructure.Services.Cache
{
    public interface IConverter<T>
    {
        string Serialize(object obj);

        T Deserialize(string value);
    }
}