﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage.Table;

namespace HipchatMTGBot
{
    class RotDCard : TableEntity
    {
        public int Version { get; set; }
        public DateTime DateShown { get; set; }
    }
}
