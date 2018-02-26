using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HipchatMTGBot
{
    internal class JsonMessageFrom
    {
        public string id { get; set; }
        public string name { get; set; }
        public string room { get; set; }
    }

    internal class JsonMessage
    {
        public JsonMessageFrom from { get; set; }
        public string message { get; set; }
    }
}
