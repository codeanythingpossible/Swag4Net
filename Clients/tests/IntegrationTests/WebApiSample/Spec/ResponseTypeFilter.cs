using System.Reflection;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.Swagger;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace WebApiSample.Spec
{
  public class ResponseTypeFilter : IOperationFilter
  {        
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
      var attr = context.MethodInfo.GetCustomAttribute<SwaggerResponseContentTypeAttribute>();
      if (attr == null)
        return;
 
      operation.Responses.Clear();

      //foreach (var mimetype in attr.Mimetypes) operation.Responses.Add(mimetype);
    }

  }
}