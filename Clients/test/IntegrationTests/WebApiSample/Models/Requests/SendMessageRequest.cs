using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebApiSample.Models.Requests
{
    public class SendMessageRequest
    {
        public Guid ToUserId { get; set; }
        public string Content { get; set; }
        public string Format { get; set; }
    }
}
