using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace ClientsForSwagger.RestClient.Serialization
{
  public interface ISerializer
  {
    bool Support(MediaTypeHeaderValue contentType);
    Task<T> Deserialize<T>(HttpResponseMessage response);
    Task<object> Deserialize(HttpResponseMessage response, Type type);
    void Serialize<T>(HttpRequestMessage request, T model);
  }
}