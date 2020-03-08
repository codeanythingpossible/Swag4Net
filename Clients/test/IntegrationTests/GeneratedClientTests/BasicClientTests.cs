using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Swag4Net.RestClient;
using GeneratedClientTests.Generated;
using Microsoft.AspNetCore.Mvc.Testing;
using Swag4Net.RestClient.Results;
using WebApiSample;
using WebApiSample.Controllers;
using Xunit;

namespace GeneratedClientTests
{
    public class BasicClientTests : WebApiTests
    {
        private readonly ValuesApiClient client;

        public BasicClientTests(CustomWebApplicationFactory<Startup> factory) : base(factory)
        {
            var httpClient = Factory.CreateClient(
                new WebApplicationFactoryClientOptions
                {
                    AllowAutoRedirect = false
                });

            client = new ValuesApiClient(httpClient);
        }
        
        [Fact]
        public async Task Get_ShouldBeOk()
        {
            Result<IEnumerable<string>> result = await client.Get();

            Assert.True(result.IsSuccess);
            IEnumerable<string> resultValue = result.Value;
            Assert.Equal(ValuesController.Values.Count, resultValue.Count());
            Assert.True(ValuesController.Values.SequenceEqual(resultValue));
        }
        
        [Fact]
        public async Task GetId_ShouldBeOk()
        {
            var result = await client.Get(0);

            var resultValue = result.Value;
            Assert.True(result.IsSuccess);
            
            Assert.Equal(ValuesController.Values.First(), resultValue);
        }
    }
}