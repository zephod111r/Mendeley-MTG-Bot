﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HipchatMTGBot
{
    class Ruling
    {
        public string date;
        public string text;
    }

    class Card
    {
        public string name;
        public string manaCost;
        public float cmc;
        public List<string> colors;
        public string type;
        public List<string> subtypes;
        public List<string> types;
        public string rarity;
        public string text;
        public string flavor;
        public string artist;
        public string number;
        public string power;
        public string toughness;
        public int multiverseid;
        public string imageName;
        public List<Ruling> rulings;
        public string layout;
        public List<string> printings;
        public List<string> colorIdentity;
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
