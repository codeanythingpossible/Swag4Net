using System;

namespace Swag4Net.RestClient.Formatters
{
  class DefaultParameterFormatter : IParameterFormatter
  {
    public bool Support(Type type) => true;

    public string Format(object value) => value == null ? string.Empty : value.ToString();
  }
}