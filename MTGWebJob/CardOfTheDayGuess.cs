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

    class CardOfTheDayGuess : TableEntry
    {
        private const String tableName = "cardofthedayguesses";
        internal override String TableName { get { return tableName; } }

        public CardOfTheDayGuess() : base(Guid.NewGuid().ToString()) { }

        public String Card { get; set; }
        public String Guess { get; set; }
        public String User { get; set; }
        public DateTime When { get; set; }
        public String SeasonId { get; set; }
        public bool WinningGuess { get; set; }

        public static List<CardOfTheDayGuess> GuessesForSeason(string seasonId)
        {
            string query2 = TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, Program.Messenger.Room);
            List<CardOfTheDayGuess> cotdGuesses = new List<CardOfTheDayGuess>();
            Program.AzureStorage.Populate<CardOfTheDayGuess>(out cotdGuesses, tableName, Program.Messenger.Room);
            return cotdGuesses.Where(g => g.SeasonId == seasonId).ToList();
        }

        public static Dictionary<string, PlayerCounts> GetPlayerCounts()
        {
            Dictionary<string, PlayerCounts> playerCounts = new Dictionary<string, PlayerCounts>();
            
            Season season = Season.Get();

            List<CardOfTheDayGuess> guesses = CardOfTheDayGuess.GuessesForSeason(season.Id);

            guesses.ForEach((CardOfTheDayGuess guess) => {
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
    }
}
