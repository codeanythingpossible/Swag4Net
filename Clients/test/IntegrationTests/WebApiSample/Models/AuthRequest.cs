using System.ComponentModel.DataAnnotations;

namespace WebApiSample.Models
{
    public class AuthRequest
    {
        [Required]
        [MaxLength(140)]
        public string Login { get; set; }
        
        public string Password { get; set; }
    }
}