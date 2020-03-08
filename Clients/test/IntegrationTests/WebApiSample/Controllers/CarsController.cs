using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Bogus;
using Bogus.DataSets;
using Bogus.Extensions;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using WebApiSample.Models;
using WebApiSample.Models.Cars;
using WebApiSample.Spec;

namespace WebApiSample.Controllers
{
  [Route("api/[controller]")]
  public class CarsController : ControllerBase
  {
    public static readonly ConcurrentDictionary<Guid, Manufacturer> ManufacturersStorage = new ConcurrentDictionary<Guid, Manufacturer>();
    public static readonly ConcurrentDictionary<Guid, CarModel> CarModelsStorage = new ConcurrentDictionary<Guid, CarModel>();
    public static readonly ConcurrentDictionary<Guid, CarOffer> CarOffersStorage = new ConcurrentDictionary<Guid, CarOffer>();

    static CarsController()
    {
      var manufacturers = new Faker<Manufacturer>()
        .RuleFor(u => u.Id, f => Guid.NewGuid())
        .RuleFor(u => u.Name, f => f.Vehicle.Manufacturer())
        .Generate(15);

      var modelFaker = new Faker<CarModel>()
        .RuleFor(u => u.Id, _ => Guid.NewGuid())
        .RuleFor(u => u.Name, f => f.Vehicle.Model());
      
      var offerFaker = new Faker<CarOffer>()
        .RuleFor(u => u.Id, _ => Guid.NewGuid())
        .RuleFor(u => u.Title, f => f.Lorem.Sentence())
        .RuleFor(u => u.ProductionDate, f => f.Date.Between(DateTime.Now.AddYears(-20), DateTime.Now).AddMonths(-2));

      foreach (var manufacturer in manufacturers)
      {
        ManufacturersStorage.TryAdd(manufacturer.Id, manufacturer);

        foreach (var model in modelFaker.GenerateBetween(10, 30))
        {
          model.Manufacturer = manufacturer;
          CarModelsStorage.TryAdd(model.Id, model);

          foreach (var offer in offerFaker.GenerateBetween(5, 100))
          {
            offer.Model = model;
            CarOffersStorage.TryAdd(offer.Id, offer);
          }
        }
      }
      
    }
    
    [Route("brands")]
    [HttpGet]
    [SwaggerResponse((int)HttpStatusCode.OK, Type = typeof(Manufacturer[]))]
    [SwaggerResponseContentType("application/json")]
    public IActionResult Brands()
    {
      return Ok(ManufacturersStorage.Values);
    }
    
    [Route("brand/{id}")]
    [HttpGet]
    [SwaggerResponse((int)HttpStatusCode.OK, Type = typeof(Manufacturer))]
    [SwaggerResponse((int)HttpStatusCode.NotFound)]
    [SwaggerResponseContentType("application/json")]
    public IActionResult GetBrand(Guid id)
    {
      if (!ManufacturersStorage.TryGetValue(id, out var brand))
        return NotFound();

      return Ok(brand);
    }
    
    [Route("brand/{id}/models")]
    [HttpGet]
    [SwaggerResponse((int)HttpStatusCode.OK, Type = typeof(CarModel[]))]
    [SwaggerResponse((int)HttpStatusCode.NotFound)]
    [SwaggerResponseContentType("application/json")]
    public IActionResult GetBrandModels(Guid id)
    {
      if (!ManufacturersStorage.TryGetValue(id, out var brand))
        return NotFound();

      return Ok(CarModelsStorage.Values.Where(m => m.Manufacturer.Id == id).ToArray());
    }
    
    [Route("brand/{brandId}/models/{modelId}")]
    [HttpGet]
    [SwaggerResponse((int)HttpStatusCode.OK, Type = typeof(CarModel))]
    [SwaggerResponse((int)HttpStatusCode.NotFound)]
    [SwaggerResponseContentType("application/json")]
    public IActionResult GetBrandModel(Guid brandId, Guid modelId)
    {
      if (!CarModelsStorage.TryGetValue(modelId, out var carModel) || carModel.Manufacturer.Id != brandId)
        return NotFound();
      
      return Ok(carModel);
    }
    
    [Route("brand/{brandId}/models/{modelId}/offers")]
    [HttpGet]
    [SwaggerResponse((int)HttpStatusCode.OK, Type = typeof(CarOffer[]))]
    [SwaggerResponse((int)HttpStatusCode.NotFound)]
    [SwaggerResponseContentType("application/json")]
    public IActionResult GetOffers(Guid brandId, Guid modelId)
    {
      if (!CarModelsStorage.TryGetValue(modelId, out var carModel) || carModel.Manufacturer.Id != brandId)
        return NotFound();

      var offers = CarOffersStorage.Values.Where(o => o.Model.Id == modelId && o.Model.Manufacturer.Id == brandId).ToArray();

      return Ok(offers);
    }
    
  }
}