using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace Hip_Chat_Mtg_Bot
{
    class Vote
    {
        private Timer timer = null;

        public string Requestor = null;

        public string Question
        {
            get;
            private set;
        }
        public Dictionary<string, string> Voters
        {
            get;
            private set;
        }
        public Dictionary<string, int> Answers
        {
            get;
            private set;
        }

        public Vote(string requestingUser, string question, string[] answers, int duration)
        {
            Requestor = requestingUser;
            Question = question;
            Voters = new Dictionary<string, string>();
            Answers = new Dictionary<string, int>();
            string announce = "<b>" + requestingUser + "</b> would like you all to vote on: <b>" + question + "</b><br/><br/>Your choices of answer are:<br/>";
            foreach (string answer in answers)
            {
                Answers.Add(answer, 0);
                announce += "<br/>" + answer;
            }
            timer = new Timer(ReportAnswersAndDelete, this, duration, Timeout.Infinite);
            
            HipchatMessenger.SendMessage(announce);
        }

        public void Answer(string user, string answer)
        {
            if(!Answers.Keys.Contains(answer))
            {
                return;
            }
            if(Voters.Keys.Contains(user))
            {
                Answers[Voters[user]] = Answers[Voters[user]] - 1;
            }
            Voters[user] = answer;
            Answers[answer] = Answers[answer] + 1;
        }

        static void ReportAnswersAndDelete(Object o)
        {
            // Reset the vote mechanic!
            Program.CurrentVote = null;

            Vote vote = (Vote)o;

            string message = "<b>Results for " + vote.Requestor + "'s vote asking:</b> " + vote.Question + "<br/><br/><table>";

            var sortedVoters = vote.Voters.OrderBy(p => p.Value);
            var sortedAnswers = vote.Answers.OrderBy(p => p.Value);
            int winner = 0;
            bool isDraw = true;

            foreach(KeyValuePair<string, string> answer in sortedVoters)
            {
                message += String.Format("<tr><td>{0}</td><td></td><td>{1}</td></tr>", answer.Key, answer.Value);
            }
            message += "</table><br/><br/>";

            foreach (KeyValuePair<string, int> answer in sortedAnswers)
            {
                if (answer.Value > winner)
                {
                    winner = answer.Value;
                    isDraw = false;
                }
                else if(winner == answer.Value)
                {
                    isDraw = true;
                }
            }

            if (!isDraw)
            {
                message += "<p>The <b>'";
                message += sortedAnswers.Where(p => p.Value == winner).First().Key;
                message += "'</b> have it!</p>";
            }
            else
            {
                var winningAnswers = vote.Answers.Where(p => p.Value == winner).ToArray();
                bool first = true;
                message += "<p>We have a tie between<b>";
                foreach(KeyValuePair<string, int> answer in winningAnswers)
                {
                    if (!first)
                    {
                        message += "</b> and<b>";
                    }
                    else
                    {
                        first = false;
                    }
                    message += " " + answer.Key;
                }
                message += "</b></p>";
            }
            HipchatMessenger.SendMessage(message);
        }
    }
}
