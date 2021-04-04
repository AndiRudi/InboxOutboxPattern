using System;

namespace InboxOutboxPattern.Service1.Models
{
    public class OutBoxItem 
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public DateTimeOffset TimeStamp { get; set; } = DateTimeOffset.Now;

        public string AggregateId { get; set; }
        public string AggregateData { get; set; }
        public DateTimeOffset Sent { get; set; }
    }   
}