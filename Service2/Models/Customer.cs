using System;

namespace InboxOutboxPattern.Service2.Models
{
    public class Customer 
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; }
    }
}