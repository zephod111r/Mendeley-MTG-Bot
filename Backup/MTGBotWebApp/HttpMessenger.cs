using System;
using System.Web;

namespace HipchatMTGBot
{
    internal class QueueMessenger : MessageClient
    {


        internal QueueMessenger()
        {
            
        }

        public override void SendMessage(string message, MessageColour colour = MessageColour.Purple)
        {
            // No Op.
        }


    }
}