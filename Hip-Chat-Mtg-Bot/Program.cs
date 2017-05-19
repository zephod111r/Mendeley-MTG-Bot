using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using HipchatApiV2;
using HipchatApiV2.Requests;
using HipchatApiV2.Responses;
using HipchatApiV2.Enums;
using Newtonsoft.Json;
using System.Web;

namespace HipchatMTGBot
{
    class Program
    {
        private static Dictionary<string, string> arguments = new Dictionary<string, string>();
        const string regexPatternUser = @"\[\[(.+)\]\]";
        const string regexPatternHelp = @"^\/(?:help|Help|HELP)$";

        static public bool Quit
        {
            private get;
            set;
        }

        static public HipchatMessenger Messenger
        {
            get;
            private set;
        }

        static public MagicTheGathering CardManager
        {
            get;
            private set;
        }

        static void Main(string[] args)
        {
            Messenger = new HipchatMessenger();
            ParseArguments(args);
            CardManager = new MagicTheGathering();
            Vote.Init();
            Messenger.SetTopic("Type '/Help' to obtain MTG Bot instructions");
            Messenger.Handle(regexPatternUser, getUserProfile);
            Messenger.Handle(regexPatternHelp, getHelp);
            Console.ReadLine();
        }

        private static string getHelp(Dictionary<string,string> options, string requestingUser)
        {
            Dictionary<string, string> helpItems = new Dictionary<string, string>();
            
            HipchatMessenger.GetHelp(ref helpItems);
            MagicTheGathering.GetHelp(ref helpItems);
            Vote.GetHelp(ref helpItems);

            string helpString = $"<table>";

            foreach (var item in helpItems)
            {
                helpString += $"<tr><td>{item.Key}</td><td>\t</td><td>{item.Value}</td></tr>";
            }
            helpString += $"</table>";

            return helpString;
        }

        private static string getUserProfile(string userName, string requestingUser)
        {
            userName = userName.Replace("[[", "");
            userName = userName.Replace("]]", "");
            if (String.IsNullOrEmpty(userName))
                return null;
            
            if(String.Compare(userName, "FCotD") == 0)
            {
                return Messenger.GetUserPicture("@FindlayHannam");
            }
            return Messenger.GetUserPicture(userName);
        }

        static private void ParseArguments(string[] args)
        {
            List<string> changes = new List<string>();
            foreach (string arg in args)
            {
                string[] elements = arg.Split('=');
                if (elements.Length == 2)
                {
                    if (!arguments.ContainsKey(elements[0].ToLower()))
                    {
                        arguments.Add(elements[0].ToLower(), elements[1]);
                    }
                    else
                    {
                        arguments[elements[0].ToLower()] = elements[1];
                    }
                    changes.Add(elements[0].ToLower());
                }
                else if (elements.Length == 1)
                {
                    if (elements[0].ToLower() == "quit")
                    {
                        Quit = true;
                    }
                }
            }
            UpdateChanges(changes);
        }

        static private void UpdateChanges(List<string> updates)
        {
            foreach (string change in updates)
            {
                if (change == "room")
                {
                    Messenger.Room = arguments["room"];
                }
                else if (change == "apikey")
                {
                    Messenger.ApiKey = arguments["apikey"];
                }
            }
        }

        public static Vote CurrentVote
        {
            set;
            private get;
        }
    }
}
