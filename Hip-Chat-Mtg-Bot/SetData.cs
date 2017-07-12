using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HipchatMTGBot
{
    class SetData
    {
        public string name = null;
        public string code = null;
        public string gathererCode = null;
        public string oldCode = null;
        public string releaseDate = null;
        public string border = null;
        public string type = null;
        public string block = null;
        public bool onlineOnly = false;
        public List<object> booster = null;

        public List<Card> cards = null;
    }
}
