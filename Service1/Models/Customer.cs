using System;

namespace InboxOutboxPattern.Service1.Models
{
    public class Customer 
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; }
    }
}