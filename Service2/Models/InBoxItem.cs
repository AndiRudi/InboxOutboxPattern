using System;

namespace InboxOutboxPattern.Service2.Models
{
    public class InBoxItem 
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();

        public string Data { get; set; }
        public DateTimeOffset Received { get; set; }
        public DateTimeOffset Processed { get; set; }
    } 
}