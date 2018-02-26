using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Slack.Webhooks;
using Microsoft.Azure.WebJobs;

namespace HipchatMTGBot
{
    class SlackMessenger : MessageClient
    {
        SlackClient SlackClient { get; set; }

        public SlackMessenger()
        {

        }

        public override string Topic { get; set; }

        public string WebUrl { set { SlackClient = new SlackClient(value); } } 

        public override void SendMessage(string message, MessageColour colour)
        {
            SlackMessage slackMessage = new SlackMessage();
            slackMessage.Parse = ParseMode.Full;
            slackMessage.Text = message;
            SlackClient.PostAsync(slackMessage);
        }
    }
}
