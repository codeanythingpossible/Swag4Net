using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using WebApiSample.Models;
using WebApiSample.Models.Requests;
using WebApiSample.Spec;
using UserName=System.String;

namespace WebApiSample.Controllers
{
    [Route("api/[controller]")]
    public class MessagesController : ControllerBase
    {
        public static readonly ConcurrentDictionary<UserName, User> Users = new ConcurrentDictionary<UserName, User>();

        public static readonly ConcurrentDictionary<Guid, Message> Messages = new ConcurrentDictionary<Guid, Message>();

        public static Task<User> GetUserFromId(Guid id) => Task.FromResult(Users.FirstOrDefault(u => u.Value.UserId == id).Value);

        public static Task<User> GetUserFromName(UserName name)
            => Task.FromResult(Users.GetOrAdd(name, new User { UserId = Guid.NewGuid(), UserName = name }));


        [Route("{id}")]
        [HttpGet]
        [SwaggerResponse((int)HttpStatusCode.OK, Type = typeof(Message))]
        [SwaggerResponse((int)HttpStatusCode.Forbidden)]
        [SwaggerResponse((int)HttpStatusCode.NotFound)]
        [SwaggerResponseContentType("application/json")]
        public async Task<IActionResult> Read(Guid id)
        {
            if (!User.Identity.IsAuthenticated)
                return Forbid();

            if (!Messages.TryGetValue(id, out var message))
                return NotFound();

            return Ok(message);
        }

        [Route("Send")]
        [HttpPost]
        [SwaggerResponse((int)HttpStatusCode.Accepted)]
        [SwaggerResponse((int)HttpStatusCode.Forbidden, "User not authenticated")]
        [SwaggerResponseContentType("application/json")]
        public async Task<IActionResult> Send([FromBody] SendMessageRequest messageRequest)
        {
            if (!User.Identity.IsAuthenticated)
                return Forbid();

            var currentUser = await GetUserFromName(User.Identity.Name);

            var toUser = await GetUserFromId(messageRequest.ToUserId);
            if (toUser == null)
                return BadRequest("Invalid user id");

            var message = new Message
            {
                Id = Guid.NewGuid(),
                Content = messageRequest.Content,
                Format = messageRequest.Format,
                From = currentUser,
                To = toUser
            };

            if (!Messages.TryAdd(message.Id, message))
                return StatusCode((int) HttpStatusCode.InternalServerError);

            return AcceptedAtAction("Read", new{id=message.Id});
        }


    }
}