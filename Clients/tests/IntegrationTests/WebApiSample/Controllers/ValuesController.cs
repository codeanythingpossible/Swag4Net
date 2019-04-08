using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using Swashbuckle.AspNetCore.SwaggerGen;
using WebApiSample.Spec;

namespace WebApiSample.Controllers
{
    [Route("api/[controller]")]
    public class ValuesController : ControllerBase
    {
        public static readonly List<string> Values = new List<string> { "value1", "value2" };

        [HttpGet]
        [SwaggerResponse((int)HttpStatusCode.OK, Type = typeof(IEnumerable<string>))]
        public IActionResult Get()
        {
            return Ok(Values);
        }
        
        [HttpGet("{id}")]
        [SwaggerResponse((int)HttpStatusCode.OK, Type = typeof(string))]
        [SwaggerResponse((int)HttpStatusCode.NotFound)]
        [SwaggerResponseContentType("application/json")]
        public IActionResult Get(int id)
        {
            if (Values.Count < id)
                return NotFound(id);

            return Ok(Values[id]);
        }

        [HttpPost]
        public void Post([FromBody] string value)
        {
            Values.Add(value);
        }

        [HttpPut("{id}")]
        [SwaggerResponse((int)HttpStatusCode.OK)]
        [SwaggerResponse((int)HttpStatusCode.NotFound)]
        [SwaggerResponseContentType("application/json")]
        public IActionResult Put(int id, [FromBody] string value)
        {
            if (Values.Count > id)
                return NotFound(id);

            Values[id] = value;

            return Ok();
        }

        [HttpDelete("{id}")]
        [SwaggerResponse((int)HttpStatusCode.OK)]
        [SwaggerResponse((int)HttpStatusCode.NotFound)]
        [SwaggerResponseContentType("application/json")]
        public IActionResult Delete(int id)
        {
            if (Values.Count > id)
                return NotFound(id);

            Values.RemoveAt(id);

            return Ok();
        }
    }
}