using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using HipchatApiV2;
using HipchatApiV2.Requests;
using HipchatApiV2.Responses;
using HipchatApiV2.Enums;

namespace Hip_Chat_Mtg_Bot
{
    class HipchatMessenger
    {
        const string regexPatternName = @" name:.+,";

        private static Dictionary<string, Func<string, string, string>> handlers = new Dictionary<string, Func<string, string, string>>();
        private static HipchatClient client = null;
        private static DateTime lastBeat = DateTime.Now;
        private static Timer messageTimer = null;
        private static System.Threading.Mutex mutex = new System.Threading.Mutex();
        static List<string> excludeList = new List<string>();

        /// <summary>
        /// Change this to name of Test room for teting
        /// </summary>
        static string room = "MagicTheGathering";

        public static void Init()
        {
            string apiKey = "900DYqZMDr8094BnwXwE3RFpHKdUjv5VCRJ2gSlh"; //We should pass this in via command line so that other people can use the bot without re-compiling.
            client = new HipchatClient(apiKey);
            SetTopic("MTGBot active! Useage: ((Set)) {{<Name to look for>:<number of matches you want to see>:<number of columns>}} [[User(at)identifier]] to show their Avatar.  [{<vote/ToVoteOn>:<answers , seperated>:<durationinminutes>");
            HipchatViewRoomHistoryResponse history = client.ViewRecentRoomHistory(room);
            foreach (var item in history.Items.OrderByDescending(q => q.Date))
            {
                // Ignore any pre-existing messages!
                excludeList.Add(item.Id);
            }
            messageTimer = new Timer(ViewChatHistory, client, 5000, System.Threading.Timeout.Infinite);

            Timer heartbeat = new Timer(CheckThreads, null, 45000, 45000);
        }

        private static void CheckThreads(Object o)
        {
            if (lastBeat.AddSeconds(30) < DateTime.Now)
            {
                messageTimer = new Timer(ViewChatHistory, client, 5000, System.Threading.Timeout.Infinite);
            }
        }

        public static void SendMessage(string message, RoomColors colour = RoomColors.Purple)
        {
            try
            {
                client.SendNotification(room, message, colour);
            }
            catch (Exception err)
            {
                Console.Out.WriteLineAsync(err.Message);
            }
        }

        public static void SetTopic(string message)
        {
            try
            {
                client.SetTopic(room, message);
            }
            catch (Exception err)
            {
                Console.Out.WriteLineAsync(err.Message);
            }
        }

        public static void Handle(string regexPattern, Func<string, string, string> handler)
        {
            handlers.Add(regexPattern, handler);
        }

        public static string GetUserPicture(string userName)
        {
            userName = userName.Replace("[", "");
            userName = userName.Replace("]", "");
            userName = userName.Replace("@", "");

            try
            {
                HipchatGetUserInfoResponse userResponse = client.GetUserInfo("@" + userName);
                if (userResponse.Id != 0)
                {
                    return "<img src =\"" + userResponse.PhotoUrl + "\" />";
                }
            }
            catch (Exception err)
            {
                Console.Out.WriteAsync(err.Message);
            }
            return null;
        }

        /// <summary>
        /// Pulls in chat history for the "MTG" room, ordering messages in decending Date order
        /// If message was sent within 2 seconds ago, and it matches the {{card+name}} format, get the card info and send a notification with the data.
        /// </summary>
        /// <param name="o"></param>
        private static void ViewChatHistory(Object o)
        {
            Dictionary<string, int> userCounts = new Dictionary<string, int>();
            lastBeat = DateTime.Now;
            HipchatViewRoomHistoryResponse history = new HipchatViewRoomHistoryResponse();
            var client = (HipchatClient)o;

            try
            {
                mutex.WaitOne();
                history = client.ViewRecentRoomHistory(room);

                foreach (var item in history.Items.OrderByDescending(q => q.Date))
                {
                    if(excludeList.Contains(item.Id))
                    {
                        continue;
                    }

                    excludeList.Add(item.Id);

                    if(item.Message == null)
                    {
                        continue;
                    }

                    string from = item.From;
                    from = Regex.Match(from, regexPatternName).Value;
                    from = from.Replace("name: ", "");

                    var count = 0;
                    userCounts.TryGetValue(from, out count);

                    if (count > 8)
                    {
                        continue;
                    }

                    if (item.Message == null || item.Message.Contains("((Set)) {{<Name to look for>:<number of matches you want to see>:<number of columns>}}"))
                    {
                        continue;
                    }

                    Program.SetData = "";

                    foreach( var pattern in handlers.Keys)
                    {
                        var pair = handlers.FirstOrDefault(p => p.Key == pattern);
                        foreach(Match match in Regex.Matches(item.Message, pair.Key))
                        {
                            if (!String.IsNullOrEmpty(match.Groups[1].Value) && pair.Value != null)
                            {
                                string response = pair.Value(match.Groups[1].Value, from);
                                if(response != null && response != "")
                                {
                                    SendMessage(response);
                                }
                            }
                            userCounts[from] = count+1;
                        }
                    }
                }
            }
            catch (Exception err)
            {
                Console.Out.WriteLineAsync(err.Message);
            }
            finally
            {
                mutex.ReleaseMutex();
                messageTimer = new Timer(ViewChatHistory, client, 5000, System.Threading.Timeout.Infinite);
            }
        }
    }
}
