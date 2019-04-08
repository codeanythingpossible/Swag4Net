using System;

namespace ClientsForSwagger.RestClient.Formatters
{
  public interface IParameterFormatter
  {
    bool Support(Type type);
    string Format(object value);
  }
}