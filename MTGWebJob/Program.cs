using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using System.Text.RegularExpressions;
using System.Configuration;

namespace HipchatMTGBot
{

    // To learn more about Microsoft Azure WebJobs SDK, please see https://go.microsoft.com/fwlink/?LinkID=320976
    class Program
    {
        static public string GetConnectionStringFromEnvironment(string settingName)
        {

            string settingValue = Environment.GetEnvironmentVariable(variable: settingName);

            if (settingValue == null)
            {
                settingValue = ConfigurationManager.ConnectionStrings[settingName].ConnectionString;
            }

            return settingValue;
        }

        // Please set the following connection strings in app.config for this WebJob to run:
        // AzureWebJobsDashboard and AzureWebJobsStorage
        static void Main()
        {
            Console.WriteLine("Starting Application");
            AzureStorage = new Azure();

            AzureStorage.StorageKey = GetConnectionStringFromEnvironment("AzureWebJobsStorage");
            Messenger = new QueueMessenger();
            //ParseArguments(args);
            CardManager = new MagicTheGathering();
            Vote.Init();
            Messenger.Topic = "Type '/Help' to obtain MTG Bot instructions";
            Messenger.Handle(regexPatternUser, getUserProfile);
            Messenger.Handle(regexPatternHelp, getHelp);

            var config = new JobHostConfiguration();

            if (config.IsDevelopment)
            {
                config.UseDevelopmentSettings();
            }

            var host = new JobHost(config);
            // The following code ensures that the WebJob will be running continuously
            host.RunAndBlock();
            Console.WriteLine("Terminated Application");
        }


        private static Dictionary<string, string> arguments = new Dictionary<string, string>();
        const string regexPatternUser = @"\[\[(.+)\]\]";
        const string regexPatternHelp = @"^\/(?:help|Help|HELP)$";

        static public bool Quit
        {
            private get;
            set;
        }

        static public MessageClient Messenger
        {
            get;
            private set;
        }

        static public Azure AzureStorage
        {
            get;
            private set;
        }

        static public MagicTheGathering CardManager
        {
            get;
            private set;
        }

        private static string getHelp(Dictionary<string, string> options, string requestingUser)
        {
            return helpString();
        }

        public static string helpString()
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

        private static string getUserProfile(string userName, string requestingUser)
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

        static private void ParseArguments(string[] args)
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

        static private void UpdateChanges(List<string> updates)
        {
            foreach (string change in updates)
            {
                if (change == "azurekey")
                {
                    AzureStorage.StorageKey = arguments["azurekey"];
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
