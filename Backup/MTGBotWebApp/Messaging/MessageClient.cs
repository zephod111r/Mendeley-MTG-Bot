using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;

namespace HipchatMTGBot
{
    class MessageClient : ObjectHeart
    {
        public enum MessageColour
        {
            Green,
            Red, 
            Purple,
            Yellow,
            Blue,
            Random
        }

        #region Const Values
        public const string regexParamName = @"[a-zA-Z0-9\-]+";
        public const string regexParamSeparator = @"[:=]";
        public const string regexParamValue = @"(?:""(?:[^\n\r""]+)"")|[a-zA-Z0-9\\\/.,{}]+";
        public const string regexNamedParameters = @"(" + regexParamName + @")" + regexParamSeparator + @"(" + regexParamValue + @")";
        public const string regexTableFlip = @"\(tableflip\)";

        public const string regexParameters = @"((?:(?:[\ ])" + regexParamValue + @")+)";
        public const string RegexPatternName = @" name:.+,";
        #endregion

        #region Member Values
        protected Dictionary<string, Func<Dictionary<string, string>, string, string>> m_Handlers = new Dictionary<string, Func<Dictionary<string, string>, string, string>>();
        protected Dictionary<string, Func<string, string, string>> m_HandlersAlt = new Dictionary<string, Func<string, string, string>>();
        protected Timer m_MessageTimer = null;
        protected System.Threading.Mutex m_Mutex = new System.Threading.Mutex();
        protected List<string> m_ExcludeList = new List<string>();
        protected string m_Room = "";
        #endregion

        #region Properties
        
        public static Dictionary<string, string> GetHelp(ref Dictionary<string, string> items)
        {
            return items;
        }

        public virtual string Topic { get; set; }


        /// <summary>
        /// Change this to name of Test room for testing
        /// </summary>
        public virtual string Room { get; set; }

        #endregion

        #region Public Methods

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
        public virtual string GetUserPicture(string userName) { return null; }

        public virtual void SendMessage(string message, MessageColour colour = MessageColour.Purple)
        {
            // No Op.
        }

        public bool ProcessMessage(string message, string from)
        {
            bool usedMessage = false;
            if (message == null || message.Contains(Topic))
            {
                return usedMessage;
            }

            bool cleanup = false;
            string cleanupMessage = "";
            foreach (Match match in Regex.Matches(message, regexTableFlip))
            {
                cleanupMessage += "┬─┬ノ( º _ ºノ) ";
                cleanup = true;
            }

            if (cleanup)
            {
                SendMessage(cleanupMessage, MessageColour.Green);
            }

            // If it matches one of these it is a true match
            foreach (var pattern in m_Handlers.Keys)
            {
                var pair = m_Handlers.FirstOrDefault(p => p.Key == pattern);
                foreach (Match match in Regex.Matches(message, $"^" + pair.Key))
                {
                    if (!String.IsNullOrEmpty(match.Value) && pair.Value != null)
                    {
                        Dictionary<string, string> parameters = new Dictionary<string, string>();
                        foreach (Match paramMatch in Regex.Matches(message, regexNamedParameters))
                        {
                            if (paramMatch.Groups.Count != 3)
                                continue;

                            if (paramMatch.Groups[1].Value == "" || paramMatch.Groups[1].Value == null)
                                continue;

                            string value = paramMatch.Groups[2].Value.ToLower();

                            if (Regex.Match(value, $"^\".+\"$").Success)
                            {
                                value = value.Substring(1, value.Length - 2);
                            }

                            parameters[paramMatch.Groups[1].Value.ToLower()] = value;
                        }
                        string response = pair.Value(parameters, from);
                        if (response != null && response != "")
                        {
                            SendMessage(response);
                            usedMessage = true;
                        }
                    }
                }
            }

            foreach (var oldpattern in m_HandlersAlt.Keys)
            {
                var pair = m_HandlersAlt.FirstOrDefault(p => p.Key == oldpattern);
                foreach (Match match in Regex.Matches(message, pair.Key))
                {
                    if (!String.IsNullOrEmpty(match.Groups[0].Value) && pair.Value != null && match.Groups.Count == 2 && !String.IsNullOrEmpty(match.Groups[1].Value))
                    {
                        string response = pair.Value(match.Groups[1].Value, from);
                        if (response != null && response != "")
                        {
                            SendMessage(response);
                            usedMessage = true;
                        }
                    }
                }
            }
            return usedMessage;
        }

        #endregion
    }
}
