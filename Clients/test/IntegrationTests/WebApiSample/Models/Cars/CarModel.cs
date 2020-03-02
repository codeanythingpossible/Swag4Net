using System;

namespace WebApiSample.Models.Cars
{
  public class CarModel
  {
    public Guid Id { get; set; }
    public string Name { get; set; }
    public Manufacturer Manufacturer { get; set; }
  }
}