using System.Threading.Tasks;
using Swag4Net.RestClient.Results;
using Swag4Net.RestClient.Results.DiscriminatedUnions;
using GeneratedClientTests.Generated;
using Microsoft.AspNetCore.Mvc.Testing;
using WebApiSample;
using Xunit;

namespace GeneratedClientTests
{
  public class DiscriminatedUnionResultsTests : WebApiTests
  {
    private readonly StrangeApiClient client;

    public DiscriminatedUnionResultsTests(CustomWebApplicationFactory<Startup> factory) : base(factory)
    {
            
      var httpClient = Factory.CreateClient(
        new WebApplicationFactoryClientOptions
        {
          AllowAutoRedirect = false
        });

      client = new StrangeApiClient(httpClient);
    }
    
    [Fact]
    public async Task Get_ShouldBeNothing()
    {
      Result<DiscriminatedUnion<string, Nothing, StrangeDto1>> result = await client.Get(10);
      
      Assert.True(result.IsSuccess);

      Assert.True(
        result.Value.Match(
          s => false,
          nothing => true,
          dto1 => false)
        );
    }
    
    [Fact]
    public async Task Get_NotFoundShouldFail()
    {
      Result<DiscriminatedUnion<string, Nothing, StrangeDto1>> result = await client.Get(-1);
      
      Assert.False(result.IsSuccess);
    }
    
    [Fact]
    public async Task Get_ShouldBeDto1()
    {
      Result<DiscriminatedUnion<string, Nothing, StrangeDto1>> result = await client.Get(15);
      
      Assert.True(result.IsSuccess);

      string value = result.Value.Match(
        s => s.ToUpper(),
        nothing => "",
        dto1 => dto1.Message);
      
      Assert.Equal("Some peoples make strange APIs :)", value);
    }
    
    
    [Fact]
    public async Task Get_ShouldBeString()
    {
      Result<DiscriminatedUnion<string, Nothing, StrangeDto1>> result = await client.Get(1234);
      
      Assert.True(result.IsSuccess);

      string value = result.Value.Match(
        s => s.ToUpper(),
        nothing => "",
        dto1 => dto1.Message);
      
      Assert.Equal("Everything is ok with id 1234".ToUpper(), value);
    }
    
    /*
      public Task<Result<DiscriminatedUnion<string, Nothing, StrangeDto1>>> Get(int id, CancellationToken cancellationToken = default(CancellationToken))
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "/api/Strange/{id}");
            base.AddPathParameter(request, "id", id);
            var types = new Dictionary<HttpStatusCode, Type>
            {
                {HttpStatusCode.OK, typeof(string)},
                {HttpStatusCode.NotFound, typeof(Nothing)},
                {HttpStatusCode.NoContent, typeof(Nothing)},
                {HttpStatusCode.PartialContent, typeof(StrangeDto1)},
            };
            var types = new Dictionary<HttpStatusCode, Type>();
            types.Add(HttpStatusCode.OK, typeof(string));
            types.Add(HttpStatusCode.NotFound, typeof(Nothing));
            types.Add(HttpStatusCode.NoContent, typeof(Nothing));
            types.Add(HttpStatusCode.PartialContent, typeof(StrangeDto1));
            
            return this.ExecuteDiscriminated<DiscriminatedUnion<string,Nothing,StrangeDto1>>(request, types, cancellationToken);
        }
     */
    
  }
}