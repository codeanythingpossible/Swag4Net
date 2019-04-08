using System;

namespace WebApiSample.Models.Cars
{
  public class CarOffer
  {
    public Guid Id { get; set; }
    public string Title { get; set; }
    public CarModel Model { get; set; }
    public DateTime ProductionDate { get; set; }
  }
}