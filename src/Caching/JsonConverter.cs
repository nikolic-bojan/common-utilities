using System.Text.Json;

namespace Svea.Eureka.Services.Location.Infrastructure.Services.Cache
{
    internal class JsonConverter<T> : IConverter<T>
    {
        public T Deserialize(string value)
        {
            T result = default;

            if (value != null)
            {
                result = JsonSerializer.Deserialize<T>(value);
            }

            return result;
        }

        public string Serialize(object obj)
        {
            return JsonSerializer.Serialize(obj);
        }
    }
}