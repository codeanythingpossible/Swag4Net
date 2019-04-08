using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Newtonsoft.Json;
using GeneratedClientTests.Generated;
using ClientsForSwagger.RestClient.Results.DiscriminatedUnions;
using ClientsForSwagger.RestClient.Results;
using ClientsForSwagger.RestClient;

namespace GeneratedClientTests.Generated
{
    public interface IApiClient
    {
    }

    public class ApiClient : RestApiClientBase, IApiClient
    {
        public ApiClient(string baseUrl): base(baseUrl)
        {
        }

        public ApiClient(Uri baseUrl): base(baseUrl)
        {
        }

        public ApiClient(HttpClient client): base(client)
        {
        }
    }

    public class CarsApiClient : RestApiClientBase, ICarsApiClient
    {
        public CarsApiClient(string baseUrl): base(baseUrl)
        {
        }

        public CarsApiClient(Uri baseUrl): base(baseUrl)
        {
        }

        public CarsApiClient(HttpClient client): base(client)
        {
        }

        public Task<Result<IEnumerable<Manufacturer>>> Brands(CancellationToken cancellationToken = default(CancellationToken))
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "/api/Cars/brands");
            return this.Execute<IEnumerable<Manufacturer>>(request, cancellationToken);
        }

        public Task<Result<Manufacturer>> GetBrand(string id, CancellationToken cancellationToken = default(CancellationToken))
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "/api/Cars/brand/{id}");
            base.AddPathParameter(request, "id", id);
            return this.Execute<Manufacturer>(request, cancellationToken);
        }

        public Task<Result<IEnumerable<CarModel>>> GetBrandModels(string id, CancellationToken cancellationToken = default(CancellationToken))
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "/api/Cars/brand/{id}/models");
            base.AddPathParameter(request, "id", id);
            return this.Execute<IEnumerable<CarModel>>(request, cancellationToken);
        }

        public Task<Result<CarModel>> GetBrandModel(string brandId, string modelId, CancellationToken cancellationToken = default(CancellationToken))
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "/api/Cars/brand/{brandId}/models/{modelId}");
            base.AddPathParameter(request, "brandId", brandId);
            base.AddPathParameter(request, "modelId", modelId);
            return this.Execute<CarModel>(request, cancellationToken);
        }

        public Task<Result<IEnumerable<CarOffer>>> GetOffers(string brandId, string modelId, CancellationToken cancellationToken = default(CancellationToken))
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "/api/Cars/brand/{brandId}/models/{modelId}/offers");
            base.AddPathParameter(request, "brandId", brandId);
            base.AddPathParameter(request, "modelId", modelId);
            return this.Execute<IEnumerable<CarOffer>>(request, cancellationToken);
        }
    }

    public interface ICarsApiClient
    {
        Task<Result<IEnumerable<Manufacturer>>> Brands(CancellationToken cancellationToken = default(CancellationToken));
        Task<Result<Manufacturer>> GetBrand(string id, CancellationToken cancellationToken = default(CancellationToken));
        Task<Result<IEnumerable<CarModel>>> GetBrandModels(string id, CancellationToken cancellationToken = default(CancellationToken));
        Task<Result<CarModel>> GetBrandModel(string brandId, string modelId, CancellationToken cancellationToken = default(CancellationToken));
        Task<Result<IEnumerable<CarOffer>>> GetOffers(string brandId, string modelId, CancellationToken cancellationToken = default(CancellationToken));
    }

    public class MessagesApiClient : RestApiClientBase, IMessagesApiClient
    {
        public MessagesApiClient(string baseUrl): base(baseUrl)
        {
        }

        public MessagesApiClient(Uri baseUrl): base(baseUrl)
        {
        }

        public MessagesApiClient(HttpClient client): base(client)
        {
        }

        public Task<Result<Message>> Read(string id, CancellationToken cancellationToken = default(CancellationToken))
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "/api/Messages/{id}");
            base.AddPathParameter(request, "id", id);
            return this.Execute<Message>(request, cancellationToken);
        }

        public Task<Result> Send(SendMessageRequest messageRequest, CancellationToken cancellationToken = default(CancellationToken))
        {
            var request = new HttpRequestMessage(HttpMethod.Post, "/api/Messages/Send");
            base.AddBodyParameter(request, "messageRequest", messageRequest);
            return this.Execute(request, cancellationToken);
        }
    }

    public interface IMessagesApiClient
    {
        Task<Result<Message>> Read(string id, CancellationToken cancellationToken = default(CancellationToken));
        Task<Result> Send(SendMessageRequest messageRequest, CancellationToken cancellationToken = default(CancellationToken));
    }

    public class StrangeApiClient : RestApiClientBase, IStrangeApiClient
    {
        public StrangeApiClient(string baseUrl): base(baseUrl)
        {
        }

        public StrangeApiClient(Uri baseUrl): base(baseUrl)
        {
        }

        public StrangeApiClient(HttpClient client): base(client)
        {
        }

        public Task<Result<DiscriminatedUnion<string, Nothing, StrangeDto1>>> Get(int id, CancellationToken cancellationToken = default(CancellationToken))
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "/api/Strange/{id}");
            base.AddPathParameter(request, "id", id);
            var types = new Dictionary<int, Type>();
            types.Add(200, typeof(string));
            types.Add(204, typeof(Nothing));
            types.Add(206, typeof(StrangeDto1));
            return this.ExecuteDiscriminated<DiscriminatedUnion<string,Nothing,StrangeDto1>>(request, types, cancellationToken);
        }
    }

    public interface IStrangeApiClient
    {
        Task<Result<DiscriminatedUnion<string, Nothing, StrangeDto1>>> Get(int id, CancellationToken cancellationToken = default(CancellationToken));
    }

    public class ValuesApiClient : RestApiClientBase, IValuesApiClient
    {
        public ValuesApiClient(string baseUrl): base(baseUrl)
        {
        }

        public ValuesApiClient(Uri baseUrl): base(baseUrl)
        {
        }

        public ValuesApiClient(HttpClient client): base(client)
        {
        }

        public Task<Result<IEnumerable<string>>> Get(CancellationToken cancellationToken = default(CancellationToken))
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "/api/Values");
            return this.Execute<IEnumerable<string>>(request, cancellationToken);
        }

        public Task<Result> Post(string value, CancellationToken cancellationToken = default(CancellationToken))
        {
            var request = new HttpRequestMessage(HttpMethod.Post, "/api/Values");
            base.AddBodyParameter(request, "value", value);
            return this.Execute(request, cancellationToken);
        }

        public Task<Result<string>> Get(int id, CancellationToken cancellationToken = default(CancellationToken))
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "/api/Values/{id}");
            base.AddPathParameter(request, "id", id);
            return this.Execute<string>(request, cancellationToken);
        }

        public Task<Result> Put(int id, string value, CancellationToken cancellationToken = default(CancellationToken))
        {
            var request = new HttpRequestMessage(HttpMethod.Put, "/api/Values/{id}");
            base.AddPathParameter(request, "id", id);
            base.AddBodyParameter(request, "value", value);
            return this.Execute(request, cancellationToken);
        }

        public Task<Result> Delete(int id, CancellationToken cancellationToken = default(CancellationToken))
        {
            var request = new HttpRequestMessage(HttpMethod.Delete, "/api/Values/{id}");
            base.AddPathParameter(request, "id", id);
            return this.Execute(request, cancellationToken);
        }
    }

    public interface IValuesApiClient
    {
        Task<Result<IEnumerable<string>>> Get(CancellationToken cancellationToken = default(CancellationToken));
        Task<Result> Post(string value, CancellationToken cancellationToken = default(CancellationToken));
        Task<Result<string>> Get(int id, CancellationToken cancellationToken = default(CancellationToken));
        Task<Result> Put(int id, string value, CancellationToken cancellationToken = default(CancellationToken));
        Task<Result> Delete(int id, CancellationToken cancellationToken = default(CancellationToken));
    }
}