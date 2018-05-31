using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MTGWebJob
{
    class Ruling
    {
        public string date { get; set; }
        public string text { get; set; }
    }

    class Legality
    {
        public string format { get; set; }
        public string legality { get; set; }
    }

    class Card
    {
        public string name { get; set; }
        public string manaCost { get; set; }
        public float cmc { get; set; }
        public List<string> colors { get; set; }
        public string type { get; set; }
        public List<string> subtypes { get; set; }
        public List<string> types { get; set; }
        public string rarity { get; set; }
        public string text { get; set; }
        public string flavor { get; set; }
        public string artist { get; set; }
        public string number { get; set; }
        public string power { get; set; }
        public string toughness { get; set; }
        public int multiverseid { get; set; }
        public string imageName { get; set; }
        public List<Ruling> rulings { get; set; }
        public string layout { get; set; }
        public List<string> printings { get; set; }
        public List<string> colorIdentity { get; set; }
        public List<Legality> legalities { get; set; }
        public string mciNumber { get; set; }
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
