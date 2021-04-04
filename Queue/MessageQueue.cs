using System;

namespace InboxOutboxPattern.Queue 
{

    public class ReceivedEventArgs : EventArgs
    {
        public string Data { get; set; }

        public bool Acked { get; set;  } //Has no functionality here... :)

        public ReceivedEventArgs(string data) 
        {
            this.Data = data;
        }   

    }

    /// <summary>
    /// Simulates a message queue like rabbit
    /// </summary>
    public class MessageQueue 
    {
        public event EventHandler<ReceivedEventArgs> OnDataReceived;

        public bool SimulateError = false;

        public void Send(string data) 
        {
            if (SimulateError) throw new Exception("A network error has occurred");

            Console.WriteLine("Queue: Received Data from Service 1 and sending it to Service 2 now");
            this.OnDataReceived?.Invoke(this, new ReceivedEventArgs(data));
            Console.WriteLine("Queue: Finished sending Data to Service 2");
        }
    }
}