using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.WindowsAzure.Storage.Table;

namespace MTGWebJob
{
    class PlayerCounts
    {
        public String Player { get; set; }
        public int Count { get; set; }
        public int Correct { get; set; }
    }

    class CotdGuess : TableEntry
    {
        private const String tableName = "cotdguesses";
        public CotdGuess() : base(Program.Messenger.Room, Guid.NewGuid().ToString()) { }
        internal override String TableName { get { return tableName; } }
        public String Card { get; set; }
        public String Guess { get; set; }
        public String User { get; set; }
        public DateTime When { get; set; }
        public String SeasonId { get; set; }
        public bool WinningGuess { get; set; }

        public static List<CotdGuess> GuessesForSeason(String seasonId)
        {
            string query2 = TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, Program.Messenger.Room);
            query2 = TableQuery.CombineFilters(query2, TableOperators.And, TableQuery.GenerateFilterCondition("Season", QueryComparisons.Equal, seasonId));
            List<CotdGuess> cotdGuesses = new List<CotdGuess>();
            Program.AzureStorage.Populate<CotdGuess>(out cotdGuesses, tableName, query2);
            return cotdGuesses;
        }

        public static Dictionary<string, PlayerCounts> GetPlayerCounts()
        {
            Dictionary<string, PlayerCounts> playerCounts = new Dictionary<string, PlayerCounts>();

            SettingString seasonId = Setting.Get<SettingString>(Season.SettingName);

            List<CotdGuess> guesses = CotdGuess.GuessesForSeason(seasonId.Value);

            guesses.ForEach((CotdGuess guess) => {
                if (!playerCounts.ContainsKey(guess.User))
                {
                    playerCounts.Add(guess.User, new PlayerCounts());
                }
                playerCounts[guess.User].Count++;
                if(guess.WinningGuess)
                {
                    playerCounts[guess.User].Correct++;
                }
            });

            return playerCounts;
        }
        internal static string PrintScores(Dictionary<string, PlayerCounts> playerScores)
        {
            string ret = "Current Player Scores are:<br><table>";

            int i = 0;
            foreach (var player in playerScores.OrderByDescending(p => p.Value.Count))
            {
                ++i;
                ret += "<tr><td>" + i.ToString() + ".) </td><td>" + player.Key + "</td><td></td><td>" + player.Value.Correct.ToString() + "</td><td>    (" + player.Value.Count + ")</td></tr>";
            }
            ret += "</table>";
            return ret;
        }
    }
}
