using System.Reflection;
using Swashbuckle.AspNetCore.Swagger;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace WebApiSample.Spec
{
  public class ResponseTypeFilter : IOperationFilter
  {        
    public void Apply(Operation operation, OperationFilterContext context)
    {
      var attr = context.MethodInfo.GetCustomAttribute<SwaggerResponseContentTypeAttribute>();
      if (attr == null)
        return;
 
      operation.Produces.Clear();

      foreach (var mimetype in attr.Mimetypes) operation.Produces.Add(mimetype);
    }
  }
}