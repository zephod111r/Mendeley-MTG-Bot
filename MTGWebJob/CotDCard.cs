using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.WindowsAzure.Storage.Table;

namespace HipchatMTGBot
{
    class CotDCard : TableEntity
    {
        public DateTime DateShown { get; set; }
        public string GuessingPlayer { get; set; }
        public DateTime DateGuessed { get; set; }
    }
}
