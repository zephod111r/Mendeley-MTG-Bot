using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MTGWebJob
{
    class SetData
    {
        public string name { get; set; }
        public string code { get; set; }
        public string gathererCode { get; set; }
        public string oldCode { get; set; }
        public string releaseDate { get; set; }
        public string border { get; set; }
        public string type { get; set; }
        public string block { get; set; }
        public bool onlineOnly { get; set; }
        public List<object> booster { get; set; }
        public List<Card> cards { get; set; }
    }
}
