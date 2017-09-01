using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HipchatMTGBot
{
    class CotD
    {
        public int Version { get; set; }
        public SetData set { get; set; }
        public Card card { get; set; }
        public string display { get; set; }
    }
}
