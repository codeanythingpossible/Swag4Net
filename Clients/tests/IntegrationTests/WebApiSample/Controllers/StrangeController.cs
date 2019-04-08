using System.Net;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using WebApiSample.Spec;

namespace WebApiSample.Controllers
{
    [Route("api/[controller]")]
    public class StrangeController : ControllerBase
    {
        [HttpGet("{id}")]
        [SwaggerResponse((int)HttpStatusCode.OK, Type = typeof(string))]
        [SwaggerResponse((int)HttpStatusCode.NoContent)]
        [SwaggerResponse((int)HttpStatusCode.NotFound)]
        [SwaggerResponse((int)HttpStatusCode.PartialContent, Type = typeof(StrangeDto1))]
        [SwaggerResponseContentType("application/json")]
        public IActionResult Get(int id)
        {
            if (id <= 0)
                return NotFound();
            
            if (id == 10)
                return StatusCode((int) HttpStatusCode.NoContent);

            if (id == 15)
                return StatusCode((int) HttpStatusCode.PartialContent, new StrangeDto1
                {
                    Id = id,
                    Message = "Some peoples make strange APIs :)"
                });
            
            //return Ok($"Everything is ok with id {id}");
            
            return new ObjectResult($"Everything is ok with id {id}"); //enforce JSON
        }
        
        public class StrangeDto1
        {
            public int Id { get; set; }
            public string Message { get; set; }
        }
    }
}