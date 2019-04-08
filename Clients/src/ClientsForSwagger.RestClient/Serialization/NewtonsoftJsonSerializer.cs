using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace ClientsForSwagger.RestClient.Serialization
{
    public class NewtonsoftJsonSerializer : ISerializer
    {
        public bool Support(MediaTypeHeaderValue contentType)
        {
            if (string.IsNullOrWhiteSpace(contentType?.MediaType))
                return false;
      
            return contentType.MediaType.Equals("application/json");
        }

        public async Task<T> Deserialize<T>(HttpResponseMessage response)
        {
            return JsonConvert.DeserializeObject<T>(await response.Content.ReadAsStringAsync());
        }
        
        public async Task<object> Deserialize(HttpResponseMessage response, Type type)
        {
            var json = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject(json, type);
        }

        public void Serialize<T>(HttpRequestMessage request, T model)
        {
            var json = JsonConvert.SerializeObject(model);
            request.Content = new ByteArrayContent(Encoding.UTF8.GetBytes(json))
            {
                Headers = { ContentType = new MediaTypeHeaderValue("application/json")}
            };
        }
    }
}