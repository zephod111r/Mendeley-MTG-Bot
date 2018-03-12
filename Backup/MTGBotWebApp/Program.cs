using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace HipchatMTGBot
{
    public class Program
    {
        public static void Main(string[] args)
        {
            AzureStorage = new Azure();
            Messenger = new HipchatMessenger();
            ParseArguments(args);
            CardManager = new MagicTheGathering();
            Vote.Init();
            Messenger.Topic = "Type '/Help' to obtain MTG Bot instructions";
            Messenger.Handle(regexPatternUser, getUserProfile);
            Messenger.Handle(regexPatternHelp, getHelp);
            BuildWebHost(args).Run();
        }

        public static IWebHost BuildWebHost(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseStartup<Startup>()
                .Build();

        private static Dictionary<string, string> arguments = new Dictionary<string, string>();
        const string regexPatternUser = @"\[\[(.+)\]\]";
        const string regexPatternHelp = @"^\/(?:help|Help|HELP)$";

        static public bool Quit
        {
            private get;
            set;
        }

        static internal MessageClient Messenger
        {
            get;
            private set;
        }

        static internal SlackMessenger Slack
        {
            get;
            private set;
        }

        static internal Azure AzureStorage
        {
            get;
            private set;
        }

        static internal MagicTheGathering CardManager
        {
            get;
            private set;
        }

        static internal string getHelp(Dictionary<string, string> options, string requestingUser)
        {
            return helpString();
        }

        static internal string helpString()
        {
            Dictionary<string, string> helpItems = new Dictionary<string, string>();

            MagicTheGathering.GetHelp(ref helpItems);
            Vote.GetHelp(ref helpItems);

            string helpString = $"Help Guide for MTG Bot:<br/><table>";

            foreach (var item in helpItems)
            {
                helpString += $"<tr><td>{item.Key}</td><td>\t</td><td>{item.Value}</td></tr><tr><td><br></td></tr>";
            }
            helpString += $"</table>";

            return helpString;
        }

        static internal string getUserProfile(string userName, string requestingUser)
        {
            userName = userName.Replace("[[", "");
            userName = userName.Replace("]]", "");
            if (String.IsNullOrEmpty(userName))
                return null;

            if (String.Compare(userName, "FCotD") == 0)
            {
                return Messenger.GetUserPicture("@FindlayHannam");
            }
            return Messenger.GetUserPicture(userName);
        }

        static internal void ParseArguments(string[] args)
        {
            List<string> changes = new List<string>();
            foreach (string arg in args)
            {
                string[] elements = arg.Split('=');
                if (elements.Length >= 2)
                {
                    string value = "";

                    for (var i = 1; i < elements.Length; ++i)
                    {
                        if (i != 1) value += "=";
                        value += elements[i];
                    }

                    if (Regex.Match(value, $"^\".+\"$").Success)
                    {
                        value = value.Substring(1, value.Count() - 2);
                    }

                    if (!arguments.ContainsKey(elements[0].ToLower()))
                    {
                        arguments.Add(elements[0].ToLower(), value);
                    }
                    else
                    {
                        arguments[elements[0].ToLower()] = value;
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

        static internal void UpdateChanges(List<string> updates)
        {
            foreach (string change in updates)
            {
                if (change == "weburl")
                {
                    if (Messenger.GetType() == typeof(SlackMessenger))
                    {
                        ((SlackMessenger)Messenger).WebUrl = arguments["weburl"];
                    }
                }
                if (change == "room")
                {
                    if (Messenger.GetType() == typeof(HipchatMessenger))
                    {
                        ((HipchatMessenger)Messenger).Room = arguments["room"];
                    }
                }
                else if (change == "apikey")
                {
                    if (Messenger.GetType() == typeof(HipchatMessenger))
                    {
                        ((HipchatMessenger)Messenger).ApiKey = arguments["apikey"];
                    }
                }
                else if (change == "azurekey")
                {
                    AzureStorage.StorageKey = arguments["azurekey"];
                }
            }
        }

        static internal Vote CurrentVote
        {
            set;
            private get;
        }
    }
}
