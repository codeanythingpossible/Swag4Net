using System;
using Newtonsoft.Json;
using System.Threading;
using System.Threading.Tasks;
using Swag4Net.Sample.Generated;
using System.Net;
using System.Net.Http;
using Swag4Net.RestClient;
using Swag4Net.RestClient.Results;

namespace Swag4Net.Sample.Generated
{
    public class PetstoreClient : RestApiClientBase
    {
        public PetstoreClient(string baseUrl): base(baseUrl)
        {
        }

        public PetstoreClient(Uri baseUrl): base(baseUrl)
        {
        }

        public PetstoreClient(HttpClient client): base(client)
        {
        }

        public Task<Result> AddPet(Pet body, CancellationToken cancellationToken = default(CancellationToken))
        {
            var request = new HttpRequestMessage(HttpMethod.Post, "/pet");
            base.AddBodyParameter(request, "body", body);
            return this.Execute(request, cancellationToken);
        }

        public Task<Result> UpdatePet(Pet body, CancellationToken cancellationToken = default(CancellationToken))
        {
            var request = new HttpRequestMessage(HttpMethod.Put, "/pet");
            base.AddBodyParameter(request, "body", body);
            return this.Execute(request, cancellationToken);
        }

        public Task<Result<Pet[]>> FindPetsByStatus(string[] status, CancellationToken cancellationToken = default(CancellationToken))
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "/pet/findByStatus");
            base.AddQueryParameters(request, "status", status);
            return this.Execute<Pet[]>(request, cancellationToken);
        }

        public Task<Result<Pet[]>> FindPetsByTags(string[] tags, CancellationToken cancellationToken = default(CancellationToken))
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "/pet/findByTags");
            base.AddQueryParameters(request, "tags", tags);
            return this.Execute<Pet[]>(request, cancellationToken);
        }

        public Task<Result<Pet>> GetPetById(long petId, CancellationToken cancellationToken = default(CancellationToken))
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "/pet/{petId}");
            base.AddPathParameter(request, "petId", petId);
            return this.Execute<Pet>(request, cancellationToken);
        }

        public Task<Result> UpdatePetWithForm(long petId, string name, string status, CancellationToken cancellationToken = default(CancellationToken))
        {
            var request = new HttpRequestMessage(HttpMethod.Post, "/pet/{petId}");
            base.AddPathParameter(request, "petId", petId);
            return this.Execute(request, cancellationToken);
        }

        public Task<Result> DeletePet(string api_key, long petId, CancellationToken cancellationToken = default(CancellationToken))
        {
            var request = new HttpRequestMessage(HttpMethod.Delete, "/pet/{petId}");
            base.AddPathParameter(request, "petId", petId);
            return this.Execute(request, cancellationToken);
        }

        public Task<Result<ApiResponse>> UploadFile(long petId, string additionalMetadata, object file, CancellationToken cancellationToken = default(CancellationToken))
        {
            var request = new HttpRequestMessage(HttpMethod.Post, "/pet/{petId}/uploadImage");
            base.AddPathParameter(request, "petId", petId);
            return this.Execute<ApiResponse>(request, cancellationToken);
        }

        public Task<Result<object>> GetInventory(CancellationToken cancellationToken = default(CancellationToken))
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "/store/inventory");
            return this.Execute<object>(request, cancellationToken);
        }

        public Task<Result<Order>> PlaceOrder(Order body, CancellationToken cancellationToken = default(CancellationToken))
        {
            var request = new HttpRequestMessage(HttpMethod.Post, "/store/order");
            base.AddBodyParameter(request, "body", body);
            return this.Execute<Order>(request, cancellationToken);
        }

        public Task<Result<Order>> GetOrderById(long orderId, CancellationToken cancellationToken = default(CancellationToken))
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "/store/order/{orderId}");
            base.AddPathParameter(request, "orderId", orderId);
            return this.Execute<Order>(request, cancellationToken);
        }

        public Task<Result> DeleteOrder(long orderId, CancellationToken cancellationToken = default(CancellationToken))
        {
            var request = new HttpRequestMessage(HttpMethod.Delete, "/store/order/{orderId}");
            base.AddPathParameter(request, "orderId", orderId);
            return this.Execute(request, cancellationToken);
        }

        public Task<Result> CreateUser(User body, CancellationToken cancellationToken = default(CancellationToken))
        {
            var request = new HttpRequestMessage(HttpMethod.Post, "/user");
            base.AddBodyParameter(request, "body", body);
            return this.Execute(request, cancellationToken);
        }

        public Task<Result> CreateUsersWithArrayInput(User[] body, CancellationToken cancellationToken = default(CancellationToken))
        {
            var request = new HttpRequestMessage(HttpMethod.Post, "/user/createWithArray");
            base.AddBodyParameter(request, "body", body);
            return this.Execute(request, cancellationToken);
        }

        public Task<Result> CreateUsersWithListInput(User[] body, CancellationToken cancellationToken = default(CancellationToken))
        {
            var request = new HttpRequestMessage(HttpMethod.Post, "/user/createWithList");
            base.AddBodyParameter(request, "body", body);
            return this.Execute(request, cancellationToken);
        }

        public Task<Result<string>> LoginUser(string username, string password, CancellationToken cancellationToken = default(CancellationToken))
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "/user/login");
            base.AddQueryParameter(request, "username", username);
            base.AddQueryParameter(request, "password", password);
            return this.Execute<string>(request, cancellationToken);
        }

        public Task<Result> LogoutUser(CancellationToken cancellationToken = default(CancellationToken))
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "/user/logout");
            return this.Execute(request, cancellationToken);
        }

        public Task<Result<User>> GetUserByName(string username, CancellationToken cancellationToken = default(CancellationToken))
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "/user/{username}");
            base.AddPathParameter(request, "username", username);
            return this.Execute<User>(request, cancellationToken);
        }

        public Task<Result> UpdateUser(string username, User body, CancellationToken cancellationToken = default(CancellationToken))
        {
            var request = new HttpRequestMessage(HttpMethod.Put, "/user/{username}");
            base.AddPathParameter(request, "username", username);
            base.AddBodyParameter(request, "body", body);
            return this.Execute(request, cancellationToken);
        }

        public Task<Result> DeleteUser(string username, CancellationToken cancellationToken = default(CancellationToken))
        {
            var request = new HttpRequestMessage(HttpMethod.Delete, "/user/{username}");
            base.AddPathParameter(request, "username", username);
            return this.Execute(request, cancellationToken);
        }
    }
}