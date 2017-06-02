using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using HipchatApiV2;
using HipchatApiV2.Responses;
using HipchatApiV2.Enums;

namespace HipchatMTGBot
{
    class HipchatMessenger : ObjectHeart
    {
        #region Const Values
        public const string regexParamName = @"[a-zA-Z0-9\-]+";
        public const string regexParamSeparator = @"[:=]";
        public const string regexParamValue = @"(?:""(?:[^\n\r""]+)"")|[a-zA-Z0-9\\\/.,]+";
        public const string regexNamedParameters = @"(" + regexParamName + @")" + regexParamSeparator + @"(" + regexParamValue + @")";

        public const string regexParameters = @"((?:(?:[\ ])"+ regexParamValue + @")+)";
        const string RegexPatternName = @" name:.+,";
        #endregion

        #region Member Values
        private Dictionary<string, Func<Dictionary<string, string>, string, string>> m_Handlers = new Dictionary<string, Func<Dictionary<string, string>, string, string>>();
        private Dictionary<string, Func<string, string, string>> m_HandlersAlt = new Dictionary<string, Func<string, string, string>>();
        private Timer m_MessageTimer = null;
        private System.Threading.Mutex m_Mutex = new System.Threading.Mutex();
        private List<string> m_ExcludeList = new List<string>();
        private string m_Room = "";
        #endregion

        #region Properties
        public HipchatClient m_Client
        {
            private get;
            set;
        }

        public string Topic
        {
            get
            {
                try
                {
                    HipchatGetRoomResponse response = m_Client.GetRoom(Room);
                    return response.Topic;
                }
                catch (Exception err)
                {
                    Console.Out.WriteLineAsync(err.Message);
                }
                return "";
            }
            set
            {
                try
                {
                    m_Client.SetTopic(Room, value);
                    Console.Out.WriteLineAsync("Changed topic to: " + value);
                }
                catch (Exception err)
                {
                    Console.Out.WriteLineAsync(err.Message);
                }
            }
        }

        public static Dictionary<string, string> GetHelp(ref Dictionary<string, string> items)
        {
            return items;
        }
        /// <summary>
        /// Change this to name of Test room for teting
        /// </summary>
        public string Room
        {
            private get { return m_Room; }
            set
            {
                m_Room = value;
                InitialiseRoom();
            }
        }

        public string ApiKey
        {
            set
            {
                m_Client = new HipchatClient(value);
                InitialiseRoom();
            }
        }
        #endregion

        public HipchatMessenger()
        {
            StartHeart();
        }

        #region Overrides
        /// <summary>
        /// </summary>
        protected override void StartHeart()
        {
            m_MessageTimer = new Timer(ProcessChatHistoryDelegate, this, 10000, Timeout.Infinite);
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// </summary>
        /// <param name="message"></param>
        /// <param name="colour"></param>
        public void SendMessage(string message, RoomColors colour = RoomColors.Purple)
        {
            try
            {
                m_Client.SendNotification(Room, message, colour);
            }
            catch (Exception err)
            {
                Console.Out.WriteLineAsync(err.Message);
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="message"></param>
        public void SetTopic(string message)
        {
            try
            {
                m_Client.SetTopic(Room, message);
            }
            catch (Exception err)
            {
                Console.Out.WriteLineAsync(err.Message);
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="regexPattern"></param>
        /// <param name="handler"></param>
        public void Handle(string regexPattern, Func<Dictionary<string, string>, string, string> handler)
        {
            m_Handlers.Add(regexPattern, handler);
        }

        /// <summary>
        /// </summary>
        /// <param name="regexPattern"></param>
        /// <param name="handler"></param>
        public void Handle(string regexPattern, Func<string, string, string> handler)
        {
            m_HandlersAlt.Add(regexPattern, handler);
        }

        /// <summary>
        /// </summary>
        /// <param name="userName"></param>
        public string GetUserPicture(string userName)
        {
            userName = userName.Replace("[", "");
            userName = userName.Replace("]", "");
            userName = userName.Replace("@", "");

            try
            {
                HipchatGetUserInfoResponse userResponse = m_Client.GetUserInfo("@" + userName);
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
        #endregion

        #region Private Members
        /// <summary>
        /// </summary>
        private void InitialiseRoom()
        {
            if (Room == "" || m_Client == null) return;

            try
            {
                HipchatViewRoomHistoryResponse history = m_Client.ViewRecentRoomHistory(Room);
                m_ExcludeList.Clear();
                Console.Out.WriteLineAsync("Initialising room: " + Room);
                foreach (var item in history.Items.OrderByDescending(q => q.Date))
                {
                    // Ignore any pre-existing messages!
                    m_ExcludeList.Add(item.Id);
                }
            }
            catch (Exception err)
            {
                Console.Out.WriteLineAsync(err.Message);
            }
        }


        /// <summary>
        /// </summary>
        private void ProcessChatHistory()
        {
            Dictionary<string, int> userCounts = new Dictionary<string, int>();
            HipchatViewRoomHistoryResponse history = new HipchatViewRoomHistoryResponse();
            Beat();
            try
            {
                m_Mutex.WaitOne();
                history = m_Client.ViewRecentRoomHistory(Room);

                foreach (var item in history.Items.OrderByDescending(q => q.Date))
                {
                    if (m_ExcludeList.Contains(item.Id))
                    {
                        continue;
                    }

                    m_ExcludeList.Add(item.Id);

                    if(item.Message.Contains("Help Guide for MTG Bot:"))
                    {
                        continue;
                    }

                    if (item.Message == null)
                    {
                        continue;
                    }

                    string from = item.From;
                    from = Regex.Match(from, RegexPatternName).Value;
                    from = from.Replace("name: ", "");

                    var count = 0;
                    userCounts.TryGetValue(from, out count);

                    if (count > 8)
                    {
                        continue;
                    }

                    if (item.Message == null || item.Message.Contains(Topic))
                    {
                        continue;
                    }

                    // If it matches one of these it is a true match
                    foreach (var pattern in m_Handlers.Keys)
                    {
                        var pair = m_Handlers.FirstOrDefault(p => p.Key == pattern);
                        foreach (Match match in Regex.Matches(item.Message, $"^" + pair.Key))
                        {
                            if (!String.IsNullOrEmpty(match.Value) && pair.Value != null)
                            {
                                Dictionary<string, string> parameters = new Dictionary<string, string>();
                                foreach (Match paramMatch in Regex.Matches(item.Message, regexNamedParameters))
                                {
                                    if (paramMatch.Groups.Count != 3)
                                        continue;

                                    if (paramMatch.Groups[1].Value == "" || paramMatch.Groups[1].Value == null)
                                        continue;

                                    parameters[paramMatch.Groups[1].Value.ToLower()] = paramMatch.Groups[2].Value.ToLower();
                                }
                                string response = pair.Value(parameters, from);
                                if (response != null && response != "")
                                {
                                    SendMessage(response);
                                }
                                return;
                            }
                            userCounts[from] = count + 1;
                        }
                    }

                    foreach (var oldpattern in m_HandlersAlt.Keys)
                    {
                        var pair = m_HandlersAlt.FirstOrDefault(p => p.Key == oldpattern);
                        foreach (Match match in Regex.Matches(item.Message, pair.Key))
                        {
                            if (!String.IsNullOrEmpty(match.Groups[0].Value) && pair.Value != null && match.Groups.Count == 2 && !String.IsNullOrEmpty(match.Groups[1].Value))
                            {
                                string response = pair.Value(match.Groups[1].Value, from);
                                if (response != null && response != "")
                                {
                                    SendMessage(response);
                                }
                            }
                            userCounts[from] = count + 1;
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
                m_Mutex.ReleaseMutex();
                StartHeart();
            }
        }
        #endregion

        #region Static Members
        /// <summary>
        /// </summary>
        /// <param name="o"></param>
        private static void ProcessChatHistoryDelegate(Object o)
        {
            HipchatMessenger messenger = (HipchatMessenger)o;
            messenger.ProcessChatHistory();
        }
        #endregion
    }
}
