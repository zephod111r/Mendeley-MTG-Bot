using System;
using Newtonsoft.Json;

namespace HipchatMTGBot
{
    internal class QueueMessenger : MessageClient
    {
        public override void SendMessage(string message, MessageColour colour = MessageColour.Purple)
        {
            Console.WriteLine(message);

            JsonMessage jsonMessage = new JsonMessage();
            jsonMessage.message = message;

            Program.AzureStorage.AddQueueItem("outbound", JsonConvert.SerializeObject(jsonMessage));
        }

        #region Overrides
        /// <summary>
        /// </summary>
        protected override void StartHeart()
        {
        }
        #endregion
    }
}