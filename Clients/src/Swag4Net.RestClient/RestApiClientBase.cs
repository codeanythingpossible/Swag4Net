using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Swag4Net.RestClient.Formatters;
using Swag4Net.RestClient.Results;
using Swag4Net.RestClient.Results.DiscriminatedUnions;
using Swag4Net.RestClient.Serialization;

namespace Swag4Net.RestClient
{
    public abstract class RestApiClientBase
    {
        protected readonly HttpClient Client;
        protected readonly NameValueCollection FormData = new NameValueCollection();
        
        private readonly IList<ISerializer> serializers = new List<ISerializer>();
        private readonly Dictionary<Type, IParameterFormatter> formatters = new Dictionary<Type, IParameterFormatter>();
        private readonly IParameterFormatter defaultParameterFormatter = new DefaultParameterFormatter();
        
        protected RestApiClientBase(HttpClient client)
        {
            if (client.BaseAddress == null)
                throw new ArgumentException("BaseAddress required", nameof(client));
           
            if (!client.BaseAddress.AbsoluteUri.EndsWith("/"))
                client.BaseAddress = new Uri(client.BaseAddress.AbsoluteUri + '/');
            
            Client = client;
            serializers.Add(new NewtonsoftJsonSerializer());
            serializers.Add(new PlainTextSerializer());
        }

        public IList<ISerializer> Serializers => serializers;

        public void RegisterParameterFormatter(Type type, IParameterFormatter formatter)
        {
            //TODO: formatters should be a cache, IParameterFormatter.Support() should be used
            
            if (formatters.ContainsKey(type))
                formatters[type] = formatter;
            else
                formatters.Add(type, formatter);
        }

        protected RestApiClientBase(string baseUrl) : this(new Uri(baseUrl))
        {
      
        }
    
        protected RestApiClientBase(Uri baseUrl) : this(new HttpClient {BaseAddress = baseUrl})
        {
            
        }

        protected string QueryParameter(object obj) => obj?.ToString();

        private async Task<Result<T>> Handle<T>(HttpResponseMessage response)
        {
            if (!response.IsSuccessStatusCode)
                return Result.FailureOf<T>(response.ReasonPhrase);

            var deserializer = Serializers.FirstOrDefault(d => d.Support(response.Content.Headers.ContentType));
            if (deserializer == null)
                return Result.FailureOf<T>($"Cannot deserialize content type {response.Content.Headers.ContentType}");

            try
            {
                return Result.SuccessOf(await deserializer.Deserialize<T>(response));
            }
            catch (Exception e)
            {
                return Result.FailureOf<T>(e.ToString());
            }
        }
    
        protected Result Handle(HttpResponseMessage response)
        {
            if (!response.IsSuccessStatusCode)
                return Result.Failure(response.ReasonPhrase);
            return Result.Success();
        }

        protected void AddQueryParameters<T>(HttpRequestMessage request, string name, IEnumerable<T> queryParameters)
        {
            foreach (var parameter in queryParameters)
            {
                AddQueryParameter(request, name, parameter);
            }
        }

        protected string FormatParameter<T>(T value)
        {
            return formatters.TryGetValue(typeof(T), out var formatter)
                ? formatter.Format(value)
                : defaultParameterFormatter.Format(value);
        }

        protected void AddQueryParameter<T>(HttpRequestMessage request, string name, T queryParameter)
        {
            var start = request.RequestUri.ToString();
            if (!start.Contains("?"))
                start += "?";

            var rawValue = FormatParameter(queryParameter);
            var url = start + $"{Uri.EscapeUriString(name)}={Uri.EscapeUriString(rawValue)}";
            request.RequestUri = new Uri(url, UriKind.Relative);
        }

        protected void AddCookieParameter<T>(HttpRequestMessage request, string name, T parameter)
        {
            var rawValue = FormatParameter(parameter);
            request.Headers.Add("Cookie", $"{name}={rawValue}");
        }

        protected void AddHeaderParameter<T>(HttpRequestMessage request, string name, T parameter)
        {
            var rawValue = FormatParameter(parameter);
            request.Headers.Add(name, rawValue);
        }
        
        protected void AddFormDataParameter<T>(HttpRequestMessage request, string name, T parameter)
        {
            var rawValue = FormatParameter(parameter);
            
            //var content = request.Content as FormUrlEncodedContent;
            //if (content == null)
            //    content = new FormUrlEncodedContent(new KeyValuePair<string, string>[0]);
            FormData.Add(name, rawValue);
            
        }
        
        protected void AddPathParameter<T>(HttpRequestMessage request, string name, T parameter)
        {
            if (parameter == null)
                return;

            var rawValue = FormatParameter(parameter);
            var pattern = "{" + name + "}";
            var path = request.RequestUri.ToString().Replace(pattern, Uri.EscapeUriString(rawValue));
            request.RequestUri = new Uri(path, UriKind.Relative);
        }
        
        protected void AddBodyParameter<T>(HttpRequestMessage request, string name, T parameter)
        {
            serializers.First().Serialize(request, parameter);
        }
    
        private static void FixUri(HttpRequestMessage request)
        {
            if (!request.RequestUri.IsAbsoluteUri)
                request.RequestUri = new Uri(request.RequestUri.ToString().TrimStart('/'), UriKind.Relative);
        }
        
        protected async Task<Result<T>> ExecuteDiscriminated<T>(
            HttpRequestMessage request,
            IDictionary<int, Type> types,
            CancellationToken cancellationToken)
        {
            FixUri(request);
            
            var response = await Send(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
                return Result.FailureOf<T>(response.ReasonPhrase);

            var deserializer = Serializers.FirstOrDefault(d => d.Support(response.Content.Headers.ContentType));
            if (deserializer == null)
                return Result.FailureOf<T>($"Cannot deserialize content type {response.Content.Headers.ContentType}");

            try
            {
                if (!types.TryGetValue((int)response.StatusCode, out var type))
                    return Result.FailureOf<T>($"Missing status code {(int)response.StatusCode} in swagger.");
                var dto = await deserializer.Deserialize(response, type);

                var result = (T)typeof(T).GetConstructor(new[] {type}).Invoke(new object[]{dto});
                
                return Result.SuccessOf(result);
            }
            catch (Exception e)
            {
                return Result.FailureOf<T>(e.ToString());
            }
        }

        protected async Task<Result<T>> Execute<T>(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            FixUri(request);
            
            var response = await Send(request, cancellationToken);
            return await Handle<T>(response);
        }


        protected async Task<Result> Execute(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            FixUri(request);

            var response = await Send(request, cancellationToken);
            return Handle(response);
        }

        private async Task<HttpResponseMessage> Send(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (FormData.HasKeys())
                request.Content = new FormUrlEncodedContent(FormData.AllKeys.Select(k => new KeyValuePair<string, string>(k, FormData[k])));

            return await Client.SendAsync(request, cancellationToken);
        }
    }
}