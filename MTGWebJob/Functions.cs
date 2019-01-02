using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Hosting;
using Newtonsoft.Json;

namespace MTGWebJob
{
    public class Functions
    {
        // This function will get triggered/executed when a new message is written 
        // on an Azure Queue called queue.
        public static void ProcessQueueMessage([QueueTrigger("inbound")] string message, TextWriter log)
        {
            Console.WriteLine(message);
            try
            {
                JsonMessage element = JsonConvert.DeserializeObject<JsonMessage>(message);
                Program.Messenger.ProcessMessage(element.message, element.from.name);
            } catch(Exception)
            {
                Console.Error.WriteLine(" ... Failed!");
            }
        }
    }
}
