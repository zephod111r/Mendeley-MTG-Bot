using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Slack.Webhooks;


namespace HipchatMTGBot
{
    class SlackMessenger
    {
        SlackClient SlackClient { get; set; }

        public SlackMessenger()
        {

        }

        public string WebUrl { set { SlackClient = new SlackClient(value); } } 

        public void SendMessage(string message)
        {
            SlackMessage slackMessage = new SlackMessage();
            slackMessage.Parse = ParseMode.Full;
            slackMessage.Text = message;
            SlackClient.PostAsync(slackMessage);
        }
    }
}
