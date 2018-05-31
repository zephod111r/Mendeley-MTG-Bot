using System;
using System.Collections.Generic;
using Microsoft.WindowsAzure.Storage.Table;

namespace MTGWebJob
{
    class Player : TableEntry
    {
        internal override string TableName { get { return "cotdscore";  } }
        private static double decayRateK = Math.Log(0.916666667) / (30.14 * 24 * 60 * 60 * 1000);
        
        public int TotalScore { get; set; }
        public int CotDScore { get; set; }
        public double RankScore { get; set; }
        public DateTime CotDRequest { get; set; }
        public DateTime LastCorrectGuess { get; set; }
        public int[] SeasonScore { get; set; }
        public int[] SeasonRank { get; set; }
        public string Name { get { return RowKey; } set { RowKey = value; }  }

        internal override void UpdateVersion()
        {
            if (Version < 2)
            {
                Version = 2;
                RankScore = CotDScore;
                LastCorrectGuess = DateTime.Now.ToLocalTime();
                CotDRequest = DateTime.Now.ToLocalTime();
            }
            if (Version < 3)
            {
                Version = 3;
                TotalScore = CotDScore;
            }
            if (Version < 4)
            {
                Version = 4;
                TotalScore = 0;
                CotDScore = 0;
                LastCorrectGuess = DateTime.Now.ToLocalTime();
                CotDRequest = DateTime.Now.ToLocalTime();
            }
            if (Version < 5)
            {
                Version = 5;
                RankScore = 0;
            }
            if (Version < 6)
            {
                Version = 6;
                TotalScore += CotDScore;
                RankScore = 0;
                CotDScore = 0;
            }
            if (Version < 7)
            {
                Version = 7;
                TotalScore += CotDScore;
                RankScore = 0;
                CotDScore = 0;
            }
            if (Version < 8)
            {
                Version = 8;
                TotalScore = 0;
                RankScore = 0;
                CotDScore = 0;
            }
        }
        
        private void EvaluateRank()
        {
            TimeSpan offset = DateTime.Now.ToLocalTime() - LastCorrectGuess;
            RankScore = RankScore * Math.Pow(Math.E, offset.TotalMilliseconds * decayRateK);
        }

        internal static Player GetPlayer(String requestingUser)
        {
            string query = TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, Program.Messenger.Room);
            query = TableQuery.CombineFilters(query, TableOperators.And, TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.Equal, requestingUser));
            Player player = new Player();
            player =  Program.AzureStorage.Populate<Player>(player.TableName, query);

            if (player == null)
            {
                player = new Player();
                player.PartitionKey = Program.Messenger.Room;
                player.RowKey = requestingUser;
                player.Version = 0;
            }

            player.UpdateVersion();
            player.EvaluateRank();
            return player;
        }

        internal static List<Player> GetPlayers()
        {
            List<Player> players = new List<Player>();
            Player player = new Player();
            Program.AzureStorage.Populate<Player>(out players, player.TableName, Program.Messenger.Room);

            return players;
        }

        internal override void Save()
        {
            UpdateVersion();
            EvaluateRank();
            base.Save();
        }
    }
}
