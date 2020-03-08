using System;

namespace WebApiSample.Models
{
    public class Message
    {
        public Guid Id { get; set; }
        public User From { get; set; }
        public User To { get; set; }
        public string Content { get; set; }
        public string Format { get; set; }
    }
}