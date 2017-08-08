using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage.Table;

namespace HipchatMTGBot
{
    class Player : TableEntity
    {
        public int Version { get; set; }
        public int CotDScore { get; set; }
        public DateTime LastCorrectGuess { get; set; }
        public double RankScore { get; set; }
        public DateTime CotDRequest { get; set; }
    }
}
