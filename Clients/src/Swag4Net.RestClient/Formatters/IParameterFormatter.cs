using System;

namespace Swag4Net.RestClient.Formatters
{
  public interface IParameterFormatter
  {
    bool Support(Type type);
    string Format(object value);
  }
}