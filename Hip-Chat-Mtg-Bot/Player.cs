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
        public int CotDScore { get; set; }
    }
}
