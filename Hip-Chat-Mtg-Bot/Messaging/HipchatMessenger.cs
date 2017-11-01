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
    class HipchatMessenger : MessageClient
    {
        #region Properties
        private HipchatClient m_Client
        {
            get;
            set;
        }

        public override string Topic
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

        public new static Dictionary<string, string> GetHelp(ref Dictionary<string, string> items)
        {
            return items;
        }

        /// <summary>
        /// Change this to name of Test room for teting
        /// </summary>
        public string Room
        {
            internal get { return m_Room; }
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
            m_MessageTimer = new Timer(ProcessChatHistoryDelegate, this, 15000, Timeout.Infinite);
        }
        #endregion

        #region Public Methods


        /// <summary>
        /// </summary>
        /// <param name="colour"></param>
        public RoomColors convertColour(MessageColour colour)
        {
            Dictionary<MessageColour, RoomColors> colourMap = new Dictionary<MessageColour, RoomColors>{
                { MessageColour.Blue, RoomColors.Gray },
                { MessageColour.Yellow, RoomColors.Yellow },
                { MessageColour.Purple, RoomColors.Purple },
                { MessageColour.Green, RoomColors.Green },
                { MessageColour.Red, RoomColors.Red },
                { MessageColour.Random, RoomColors.Random }
            };

            if(colourMap.ContainsKey(colour))
            {
                return colourMap[colour];
            }

            return RoomColors.Purple;
        }

        /// <summary>
        /// </summary>
        /// <param name="message"></param>
        /// <param name="colour"></param>
        public override void SendMessage(string message, MessageColour colour = MessageColour.Purple)
        {
            try
            {
                m_Client.SendNotification(Room, message, convertColour(colour));
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
        /// <param name="userName"></param>
        public override string GetUserPicture(string userName)
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


                foreach (var item in history.Items.OrderBy(q => q.Date))
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
                    from = Regex.Match(from, MessageClient.RegexPatternName).Value;
                    from = from.Replace("name: ", "");

                    int count = 0;
                    if (userCounts.TryGetValue(from, out count) && count > 8)
                    {
                        return;
                    }

                    if(ProcessMessage(item.Message, from))
                    {
                        userCounts[from] = count + 1;
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
