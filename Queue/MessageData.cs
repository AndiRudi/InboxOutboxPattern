using System;

namespace InboxOutboxPattern.Queue
{
    public class MessageData 
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public DateTimeOffset TimeStamp { get; set; } = DateTimeOffset.Now;

        public string AggregateId { get; set; }
        public string AggregateData { get; set; }

    }
}