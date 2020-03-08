using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebApiSample.Models
{
    public class User
    {
        public Guid UserId { get; set; }
        public string UserName { get; set; }
    }
}
