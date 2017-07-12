using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HipchatMTGBot
{
    class Ruling
    {
        public string date = null;
        public string text = null;
    }

    class Card
    {
        public string name = null;
        public string manaCost = null;
        public float cmc = 0.0f;
        public List<string> colors = null;
        public string type = null;
        public List<string> subtypes = null;
        public List<string> types = null;
        public string rarity = null;
        public string text = null;
        public string flavor = null;
        public string artist = null;
        public string number = null;
        public string power = null;
        public string toughness = null;
        public int multiverseid = 0;
        public string imageName = null;
        public List<Ruling> rulings = null;
        public string layout = null;
        public List<string> printings = null;
        public List<string> colorIdentity = null;
    }

    class CardResult : IComparable
    {
        public Card card;
        public int distance;

        public bool substringMatch;

        public CardResult(Card card, int distance, bool substringMatch) 
        {
            this.card = card; this.distance = distance; this.substringMatch = substringMatch;
        }

        public int CompareTo(object obj)
        {
            if (obj == null) return 1;
            CardResult otherResult = obj as CardResult;
            if (otherResult != null) {
                if (otherResult.substringMatch == substringMatch) {
                    return this.distance.CompareTo(otherResult.distance);
                } else {
                    return otherResult.substringMatch.CompareTo(this.substringMatch);
                }
            } else {
                throw new ArgumentException("Object is not a CardResult");
            }
        }
    }
}
