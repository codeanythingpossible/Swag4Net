using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Swag4Net.RestClient.Results.DiscriminatedUnions;

namespace Swag4Net.RestClient.Serialization
{
  public class PlainTextSerializer : ISerializer
  {
    public bool Support(MediaTypeHeaderValue contentType)
    {
      if (string.IsNullOrWhiteSpace(contentType?.MediaType))
        return true;
      
      return contentType.MediaType.Equals("text/plain");
    }

    public async Task<T> Deserialize<T>(HttpResponseMessage response)
    {
      if (typeof(T) == typeof(Nothing))
        return default;
      
      return (T) await Deserialize(response, typeof(T));
    }

    public async Task<object> Deserialize(HttpResponseMessage response, Type type)
    {
      if (type == typeof(Nothing))
        return default(Nothing);
      
//      if (type != typeof(string))
//        throw new NotSupportedException($"text/plain cannot be deserialized as {type}");

      var body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

      return Convert.ChangeType(body, type);
    }

    public void Serialize<T>(HttpRequestMessage request, T model)
    {
      throw new NotImplementedException();
    }
  }
}