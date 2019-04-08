using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ClientsForSwagger.RestClient;
using ClientsForSwagger.RestClient.Results;
using GeneratedClientTests.Generated;
using Microsoft.AspNetCore.Mvc.Testing;
using WebApiSample;
using WebApiSample.Controllers;
using Xunit;

namespace GeneratedClientTests
{
  public class CallPipeLineTests : WebApiTests
  {
    private readonly CarsApiClient client;
    
    public CallPipeLineTests(CustomWebApplicationFactory<Startup> factory) : base(factory)
    {
      var httpClient = Factory.CreateClient(
        new WebApplicationFactoryClientOptions
        {
          AllowAutoRedirect = false
        });

      client = new CarsApiClient(httpClient);
    }
    
    [Fact]
    public async Task GetBrandThenModelsThenOffers_ShouldBeOk()
    {
      var result = await client.Brands()
        .ThenMany(b => client.GetBrandModels(b.Id))
        .ThenMany(m => client.GetOffers(m.Manufacturer.Id, m.Id));
      
      Assert.True(result.IsSuccess);
      Assert.Equal(CarsController.CarOffersStorage.Count, result.Value.Count());
    }
    
    [Fact]
    public async Task GetBrandThenModelsThenOffers_ShouldFail()
    {
      var result = await client.Brands()
        .ThenMany(b => client.GetBrandModels(Guid.NewGuid().ToString()))
        .ThenMany(m => client.GetOffers(m.Manufacturer.Id, m.Id));
      
      Assert.False(result.IsSuccess);
      Assert.Throws<InvalidOperationException>(() => result.Value);
    }
    
  }
}