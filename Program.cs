using System;
using System.Linq;
using System.Threading;
using Hangfire;
using Hangfire.MemoryStorage;
using InboxOutboxPattern.Queue;
using Microsoft.EntityFrameworkCore;

namespace InboxOutboxPattern
{
    class Program
    {
        public static MessageQueue messageQueue = null;
        public static Service1.ServiceDbContext service1DbContext = null;
        public static Service1.Service service1 = null;
        public static Service2.ServiceDbContext service2DbContext = null;
        public static Service2.Service service2 = null;
        static BackgroundJobServer server = null;

        static void Main(string[] args)
        {
            //We are not really using hangfire for this demo, but this is how we would use it
            //GlobalConfiguration.Configuration.UseMemoryStorage();
            //GlobalConfiguration.Configuration.UseActivator(new StaticJobActivator());
            //server = new BackgroundJobServer();
            //Hangfire.RecurringJob.AddOrUpdate<Service1.Service>(s => s.ProcessAllOutbox(), Cron.Minutely());
            //Hangfire.RecurringJob.AddOrUpdate<Service2.Service>(s => s.ProcessAllInbox(), Cron.Minutely());

            messageQueue = new MessageQueue();
            service1DbContext = new Service1.ServiceDbContext();
            service1DbContext.Database.Migrate();
            service1 = new Service1.Service(service1DbContext, messageQueue);

            service2DbContext = new Service2.ServiceDbContext();
            service2DbContext.Database.Migrate();
            service2 = new Service2.Service(service2DbContext, messageQueue);

            bool exit = false;
            while (exit == false)
            {
                Console.WriteLine("Cleaning Databases");

                service1DbContext.Customers.RemoveRange(service1DbContext.Customers.ToList());
                service1DbContext.OutboxItems.RemoveRange(service1DbContext.OutboxItems.ToList());
                service1DbContext.SaveChanges();

                service2DbContext.Customers.RemoveRange(service2DbContext.Customers.ToList());
                service2DbContext.InboxItems.RemoveRange(service2DbContext.InboxItems.ToList());
                service2DbContext.SaveChanges();
                
                Console.Clear();
                
                Console.WriteLine("****************** Inbox Outbox Menu ****************");
                Console.WriteLine("1 - Without Outbox and Queue is failing");
                Console.WriteLine("2 - With Outbox, queue is working, without Inbox, deserialization not failing");
                Console.WriteLine("3 - With Outbox, queue is working, without Inbox, deserialization failing");
                Console.WriteLine("4 - With Outbox, queue is working, with Inbox, deserialization failing, then retry");

                var key = Console.ReadKey();
                switch (key.Key) 
                {
                    case ConsoleKey.D1: Test1_WithoutOutbox(); break;
                    case ConsoleKey.D2: Test2_WithOutbox(); break;
                    case ConsoleKey.D3: Test3_WithOutboxWithoutInbox(); break;
                    case ConsoleKey.D4: Test4_WithOutboxWithInbox(); break;
                    default: exit = true; break;
                }
            }
        }

        public static void Test1_WithoutOutbox() 
        {
            // Now we simulate sending one customer to service2 without an outbox and the queue will fail
            try
            {
                messageQueue.SimulateError = true;
                service1.AddCustomer_WithoutOutbox();
                service2.UseInbox = false;
            }
            catch (Exception) { }

            // We will have the customer saved in service1 but not in service2.

            Console.WriteLine($"Service1 Customers: {service1DbContext.Customers.Count()}");
            Console.WriteLine($"Service2 Customers: {service2DbContext.Customers.Count()}");
            Console.WriteLine($"That's odd. Systems are now out of sync");

            // Let's simulate the queue is back and wait a bit
            messageQueue.SimulateError = false;

            service1.ProcessAllOutbox(); //Let's simulate how hangfire would process all items once a minute

            Console.WriteLine($"Service1 Customers: {service1DbContext.Customers.Count()}");
            Console.WriteLine($"Service2 Customers: {service2DbContext.Customers.Count()}");
            Console.WriteLine($"That bad. Systems have not recovered. Still out of sync");

        }


        public static void Test2_WithOutbox()
        {
            // Now we simulate sending one customer to service2 with an inbox and the queue will fail
            messageQueue.SimulateError = true;
            service1.AddCustomer_WithOutbox();
            service2.UseInbox = false;

            try 
            {
                service1.ProcessAllOutbox(); //Let's simulate how hangfire would process all items once a minute
            } 
            catch(Exception) 
            { 
                //ignore 
            }
            

            // We will have the customer saved in service1 but not in service2.

            Console.WriteLine($"Service1 Customers: {service1DbContext.Customers.Count()}");
            Console.WriteLine($"Service2 Customers: {service2DbContext.Customers.Count()}");
            Console.WriteLine($"That's odd. Systems are now out of sync");

            // Let's simulate the queue is back and wait a bit
            messageQueue.SimulateError = false;

            try
            {
                service1.ProcessAllOutbox(); //Let's simulate how hangfire would process all items once a minute
            }
            catch (Exception)
            { 
                //ignore 
            }

            Console.WriteLine($"Service1 Customers: {service1DbContext.Customers.Count()}");
            Console.WriteLine($"Service2 Customers: {service2DbContext.Customers.Count()}");
            Console.WriteLine($"That good. Systems have recovered");
        }

        public static void Test3_WithOutboxWithoutInbox()
        {
            // Now we simulate sending one customer to service2 with an outbox and no inbox
            service2.UseInbox = false;
            service2.SimulateFailingDeserialize = true;
            service1.AddCustomer_WithOutbox();

            try
            {
                service1.ProcessAllOutbox(); //Let's simulate how hangfire would process all items once a minute
            }
            catch (Exception)
            {
                //ignore 
            }

            // We will have the customer saved in service1 but not in service2.
            Console.WriteLine($"Service1 Customers: {service1DbContext.Customers.Count()}");
            Console.WriteLine($"Service2 Customers: {service2DbContext.Customers.Count()}");
            Console.WriteLine($"That's odd. Systems are now out of sync and the message remains in the MessageQueue. Hope it will stay there until we fixed the error");

            // Let's simulate the queue is back and wait a bit
            service2.SimulateFailingDeserialize = true;

            try
            {
                service2.ProcessAllInbox(); //Let's simulate how hangfire would process all items once a minute
            }
            catch (Exception)
            {
                //ignore 
            }

            Console.WriteLine($"Service1 Customers: {service1DbContext.Customers.Count()}");
            Console.WriteLine($"Service2 Customers: {service2DbContext.Customers.Count()}");
            Console.WriteLine($"That bad. Systems have not recovered, because a retry to grab the message from the queue does not happen automatically");

            // We can simulate to process the message again by forcing the queue             
        }

        public static void Test4_WithOutboxWithInbox()
        {
            // Now we simulate sending one customer to service2 with an outbox and and inbox and we simulate
            // that Service2 has an issue while deserializing the incoming message
            service2.UseInbox = true;
            service2.SimulateFailingDeserialize = true;
            service1.AddCustomer_WithOutbox();

            try
            {
                service1.ProcessAllOutbox(); //Let's simulate how hangfire would process all items once a minute
            }
            catch (Exception)
            {
                //ignore 
            }
           
            Console.WriteLine($"Service1 Customers: {service1DbContext.Customers.Count()}");
            Console.WriteLine($"Service2 Customers: {service2DbContext.Customers.Count()}");
            Console.WriteLine($"That's odd. Systems are now out of sync. But at least the message is not anymore in the queue, we have it in out inbox");

            // Let's simulate the database is back and wait a bit
            service2.SimulateFailingDeserialize = false;
            
            try
            {
                service2.ProcessAllInbox(); //Let's simulate how hangfire would process all items once a minute
            }
            catch (Exception)
            {
                //ignore 
            }

            Console.WriteLine($"Service1 Customers: {service1DbContext.Customers.Count()}");
            Console.WriteLine($"Service2 Customers: {service2DbContext.Customers.Count()}");
            Console.WriteLine($"That good. Systems are in sync again");

        }


    }
}
