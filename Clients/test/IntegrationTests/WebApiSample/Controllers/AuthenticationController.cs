using System.Net.Http;
using Microsoft.AspNetCore.Mvc;
using WebApiSample.Models;

namespace WebApiSample.Controllers
{
    [Route("api/[controller]")]
    public class AuthenticationController : ControllerBase
    {
        [HttpPost]
        [ActionName("Login")]
        public IActionResult Login(AuthRequest authRequest)
        {
            return Ok();
        }
    }
}