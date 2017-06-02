using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using static HipchatMTGBot.HipchatMessenger;

namespace HipchatMTGBot
{
    class Vote
    {
        const string regexAsk = @"Ask|ask|ASK";
        const string regexVote = @"vote|Vote|VOTE";
        const string regexPatternQuestion = @"\/(" + regexAsk + ") " + HipchatMessenger.regexNamedParameters;
        const string regexPatternVote = @"^\/(?:" + regexVote + ") " + HipchatMessenger.regexParameters;
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

        static private Vote CurrentVote
        {
            get;
            set;
        }

        static public void Init()
        {
            Program.Messenger.Handle(regexPatternVote, voteChoice);
            Program.Messenger.Handle(regexPatternQuestion, createVote);
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
            timer = new Timer(ReportAnswersAndDelete, this, duration * 1000, Timeout.Infinite);
            
            Program.Messenger.SendMessage(announce, HipchatApiV2.Enums.RoomColors.Green);
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
            Program.Messenger.SendMessage(message);
        }

        private static string voteChoice(string vote, string requestingUser)
        {
            if(CurrentVote != null)
            {
                CurrentVote.Answer(requestingUser, vote);
            }
            return "";
        }

        private static string createVote(Dictionary<string, string> vote, string requestingUser)
        {
            if (CurrentVote != null)
                return null;

            if (vote.Count == 0)
                return null;
            
            if(vote.Keys.Contains("question") == false)
            {
                return "";
            }

            string[] options = { "yes", "no" };
            int duration = 30;

            if (vote.Keys.Contains("answers") == true)
            {
                string optionString = vote["answers"];
                optionString = optionString.Replace("\"", "");
                options = optionString.Split(',');
            }

            if(vote.Keys.Contains("duration") == true)
            {
                int.TryParse(vote["duration"], out duration);

                if(duration > 3600)
                {
                    duration = 3600;
                }

                if(duration < 300)
                {
                    duration = 300;
                }
            }

            CurrentVote = new Vote(requestingUser, vote["question"], options, duration);
            return "";
        }

        public static Dictionary<string, string> GetHelp(ref Dictionary<string, string> items)
        {
            items.Add("/" + regexAsk + @" question=""<your question>"" ?duration=<duration in seconds> ?answers=<answerchoices>", @"question is the question you are asking<br>duration is Max(3600, Min(300, duration))<br>answerchoices is a comma seperated list of potential choices. For answers with spaces double quotes are required ""Like this""");
            items.Add("/" + regexVote + @" <yourchoice>", @"yourchoice must exactly match one of the questions possible answers.");
            return items;
        }
             
    }
}
