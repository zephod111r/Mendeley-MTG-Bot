using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MTGWebJob
{
    class ForeignData
    {
        public string flavorText { get; set; }
        public string language { get; set; }
        public int multiverseId { get; set; }
        public string name { get; set; }
        public string text { get; set; }
        public string type { get; set; }
    }

    class Ruling
    {
        public string date { get; set; }
        public string text { get; set; }
    }

    class Legality
    {
        [JsonProperty(PropertyName = "1v1")]
        public string oneVone { get; set; }
        public string commander { get; set; }
        public string duel { get; set; }
        public string legacy { get; set; }
        public string modern { get; set; }
        public string penny { get; set; }
        public string vintage { get; set; }
    }

    class Card
    {
        public string artist { get; set; }
        public string borderColor { get; set; }
        public List<string> colorIdentity { get; set; }
        public List<string> colors { get; set; }
        public float convertedManaCost { get; set; }
        public List<ForeignData> foreignData { get; set; }
        public string frameVersion { get; set; }
        public bool hasFoil { get; set; }
        public string layout { get; set; }
        public Legality legalities { get; set; }
        public string manaCost { get; set; }
        public int multiverseid { get; set; }
        public string name { get; set; }
        public string number { get; set; }
        public string originalText { get; set; }
        public string originalType { get; set; }
        public string power { get; set; }
        public List<string> printings { get; set; }
        public string rarity { get; set; }
        public List<Ruling> rulings { get; set; }
        public Guid scryfallid { get; set; }
        public List<string> subtypes { get; set; }
        public List<string> supertypes { get; set; }
        public int tcgplayerProductId { get; set; }
        public string text { get; set; }
        public string toughness { get; set; }
        public string type { get; set; }
        public List<string> types { get; set; }
        public Guid uuid { get; set; }
        public List<string> variations { get; set; }
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
