using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IBMMQDemo
{
    class Program
    {
        static void Main(string[] args)
        {
            //string topic = "AddCriminalCase.request";
            //string topic = "AddCriminalCase.response";
            string topic = "PARTY.BASE.TOPIC";
            //string subscriber = "AddCriminalCase.result";
            string subscriber = "PARTY.SUB.1";
            string queue = "PARTY.QUEUE.1";
            string xml
                = "<Test>Gayathri testing " + DateTime.Now.ToString() + "</Test>";

            // Check if blank subscribe to start subscriber....
            xml = PubSubOperations.GetMessageText(topic, subscriber);
            Console.WriteLine("Subscription received message: " + xml);

            xml
                = "<Test>Gayathri testing " + DateTime.Now.ToString() + "</Test>";

            if (PubSubOperations.SendMessage(topic, xml))
                Console.WriteLine("Message: " + xml + " posted to topic.");
            else
                Console.WriteLine("Message FAILED to be posted to topic.");
            System.Threading.Thread.Sleep(5000);

            xml = PubSubOperations.GetMessageText(topic, subscriber);
            Console.WriteLine("Subscription received message: " + xml);

            xml
                = "<Test>Gayathri testing " + DateTime.Now.ToString() + "</Test>";
            if (PutPullOperations.SendMessage(queue, xml))
                Console.WriteLine("Message: " + xml + " sent to queue.");
            else
                Console.WriteLine("Message FAILED to be sent to topic.");
            System.Threading.Thread.Sleep(5000);

            xml = PutPullOperations.GetMessage(queue);
            Console.WriteLine("Queue message: " + xml);

            Console.WriteLine("Done");
            Console.ReadLine();
        }
    }
}
