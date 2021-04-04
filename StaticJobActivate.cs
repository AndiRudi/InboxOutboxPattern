using System;
using Hangfire;

namespace InboxOutboxPattern
{
    public class StaticJobActivator : JobActivator
    {
        
        public StaticJobActivator()
        {
            
        }

        public override object ActivateJob(Type type)
        {
            if (type.FullName == "InboxOutboxPattern.Service1.ServiceDbContext") return Program.service1DbContext;
            if (type.FullName == "InboxOutboxPattern.Service1.Service") return Program.service1;
            if (type.FullName == "InboxOutboxPattern.Service2.ServiceDbContext") return Program.service2DbContext;
            if (type.FullName == "InboxOutboxPattern.Service2.Service") return Program.service2;
            if (type.FullName == "InboxOutboxPattern.Queue.MessageQueue") return Program.messageQueue;

            throw new Exception($"Cannot find an instance for the specified type {type.FullName}");

        }

    }

}