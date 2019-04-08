using System;

namespace WebApiSample.Spec
{
  public class SwaggerResponseContentTypeAttribute : Attribute
  {
    public SwaggerResponseContentTypeAttribute(params string[] mimetypes)
    {
      Mimetypes = mimetypes;
    }

    public string[] Mimetypes { get; }

  }
}