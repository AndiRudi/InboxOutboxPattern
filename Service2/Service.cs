using System;
using System.Linq;
using InboxOutboxPattern.Queue;

namespace InboxOutboxPattern.Service2
{
    public class Service
    {
        private readonly MessageQueue _queue;
        private readonly ServiceDbContext _context;

        public bool UseInbox = true;

        public bool SimulateFailingDeserialize = false;

        public Service(ServiceDbContext context, MessageQueue queue)
        {
            _queue = queue;
            _context = context;

            _queue.OnDataReceived += OnDataReceived;
        }

        public void OnDataReceived(object sender, ReceivedEventArgs dataReceived) 
        {
            if (this.UseInbox) this.ProcessWithInbox(dataReceived.Data);
            else this.ProcessWithoutInbox(dataReceived.Data);

            //If no error while processing mark acked. If not acked it will be retried
            dataReceived.Acked = true;
        }


        public void ProcessWithoutInbox(string data)
        {
            //Without Inbox we directly transform the incoming data. This can fail when the data
            //cannot be deserialized. In case of an error the data remains in the queue and we can retry so
            //it is technically not an issue, but the longer the data remains in the queue the more
            //changes of loosing it we have. Also if you have the data in the inbox, you haver it easier to debug why
            //the deserialization failed.

            var messageData = System.Text.Json.JsonSerializer.Deserialize<MessageData>(data);
            if (SimulateFailingDeserialize) throw new Exception("Deserialization failed");
            var customer = System.Text.Json.JsonSerializer.Deserialize<Models.Customer>(messageData.AggregateData); 

            _context.Customers.Add(customer);
            _context.SaveChanges();
        }

        public void ProcessWithInbox(string data)
        {
            //With Inbox we just store the data as it is and the processing will be done later. Advantages are 
            //that there is less code to fail and we also got it out from the queue as soon as possible

            var inbox = new Models.InBoxItem { Data = data };
            _context.InboxItems.Add(inbox);
           _context.SaveChanges();
        }

        public void ProcessAllInbox()
        {
            //Lock this method
            var items = _context.InboxItems
                .Where(o => o.Processed == DateTimeOffset.MinValue)
                .OrderBy(o => o.Received);

            items.Take(500)
                .ToList()
                .ForEach(inBoxItem =>
                {
                    var messageData = System.Text.Json.JsonSerializer.Deserialize<MessageData>(inBoxItem.Data);
                    if (SimulateFailingDeserialize) throw new Exception("Deserialization failed");
                    var customer = System.Text.Json.JsonSerializer.Deserialize<Models.Customer>(messageData.AggregateData);

                    _context.Customers.Add(customer);
                    inBoxItem.Processed = DateTimeOffset.Now;
                    _context.SaveChanges();

                    //If something fails here, we don't care because it will be retried when the method is called again

                });

            //If there are more than 500 items in the queue directly enqueue a follow up
            if (items.Count() > 500) Hangfire.BackgroundJob.Enqueue<Service2.Service>(s => s.ProcessAllInbox());
        }


    }

}