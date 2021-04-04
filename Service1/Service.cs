using System;
using System.Linq;
using Hangfire;
using InboxOutboxPattern.Queue;

namespace InboxOutboxPattern.Service1
{
    /// <summary>
    /// Service1 sends a customer to Service2
    /// </summary>
    public class Service
    {

        private readonly MessageQueue _queue;
        private readonly ServiceDbContext _context;

        public Service(ServiceDbContext context, MessageQueue queue)
        {
            _queue = queue;
            _context = context;
        }


        public void AddCustomer_WithoutOutbox()
        {

            var customer = new Models.Customer
            {
                Name = "Andreas"
            };
            _context.Customers.Add(customer);

            _context.SaveChanges();

            var message = new MessageData
            {
                Id = Guid.NewGuid().ToString(),
                TimeStamp = DateTimeOffset.Now,
                AggregateId = customer.Id,
                AggregateData = System.Text.Json.JsonSerializer.Serialize(customer),
            };
            _queue.Send(System.Text.Json.JsonSerializer.Serialize(message));
            
            // Btw.: If you think you can just put the _context.SaveChanges() here after the sending to the queue, let me tell you, you're wrong :) 
            // What if adding to the queue succeeds, but then the database fails? The local system is the leading system, so first you need to save 
            // thingds locally, then sending it out 

        }

        public void AddCustomer_WithOutbox_WithIndividualProcessing()
        {
            var customer = new Models.Customer
            {
                Name = "Andreas"
            };

            _context.Customers.Add(customer);

            var outboxItem = new Models.OutBoxItem
            {
                AggregateId = customer.Id,
                AggregateData = System.Text.Json.JsonSerializer.Serialize(customer)
            };

            _context.OutboxItems.Add(outboxItem);

            _context.SaveChanges();

            //To speed up sending the message we can call the outboxprocessor to immediately process
            //the item. But this will violate the order of sending so you should take care if you really need this.
            //For example if you directly send a message to an email sender, you maybe don't care about the order, but want the email to be sent as fast as possible
            Hangfire.BackgroundJob.Enqueue<Service1.Service>(s => s.ProcessOutboxItem(outboxItem.Id));
        }

        public void AddCustomer_WithOutbox()
        {
            var customer = new Models.Customer
            {
                Name = "Andreas"
            };

            _context.Customers.Add(customer);

            var outboxItem = new Models.OutBoxItem
            {
                AggregateId = customer.Id,
                AggregateData = System.Text.Json.JsonSerializer.Serialize(customer)
            };

            _context.OutboxItems.Add(outboxItem);

            _context.SaveChanges();

            //The outbox processor is run automatically so nothing to do
        }


        public void ProcessOutboxItem(string outboxItemId)
        {
            //Lock this id
            var outBoxItem = _context.OutboxItems
                .Where(o => o.Sent == null)
                .SingleOrDefault(o => o.Id == outboxItemId);

            if (outBoxItem == null) return;

            
            var message = new MessageData
            {
                Id = outBoxItem.Id,
                TimeStamp = outBoxItem.TimeStamp,
                AggregateId = outBoxItem.AggregateId,
                AggregateData = outBoxItem.AggregateData
            };
            _queue.Send(System.Text.Json.JsonSerializer.Serialize(message));

            outBoxItem.Sent = DateTimeOffset.Now;
            _context.SaveChanges(); 

            //Note: If the database would fail after we have sent this outbox item, it will be sent again at some time later. Due
            //to this it is important, that the receiver checks if the record was already processes and/or if the timestamp is to old
        }

        public void ProcessAllOutbox()
        {
            //Lock this method
            var items = _context.OutboxItems
                .Where(o => o.Sent == DateTimeOffset.MinValue)
                .OrderBy(o => o.TimeStamp);
                            
            items.Take(500)
                .ToList()
                .ForEach(outBoxItem =>
                {
                    _queue.Send(System.Text.Json.JsonSerializer.Serialize(outBoxItem));
                    outBoxItem.Sent = DateTimeOffset.Now;
                    _context.SaveChanges();
                    
                    //If something fails here, we don't care because it will be retried when the method is called again

                });

            //If there are more than 500 items in the queue directly enqueue a follow up
            if (items.Count() > 500) Hangfire.BackgroundJob.Enqueue<Service1.Service>(s => s.ProcessAllOutbox());
        }



    }

}