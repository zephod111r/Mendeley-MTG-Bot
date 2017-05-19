using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Headers;

namespace HipchatMTGBot
{
    class FuzzyMatch
    {
        private const int MAXCARDNAME = 256;
        private static UInt16[,] d = new UInt16[MAXCARDNAME, MAXCARDNAME];

        public static Card BestMatch2(Dictionary<string, SetData> cardJson, string cardname)
        {
            int leastDistance = int.MaxValue;
            bool isSubstringMatch;
            CardResult match = new CardResult(cardJson.Values.ElementAt<SetData>(0).cards[0], int.MaxValue, false);
            int curDistance = 0;
            string cnu = cardname.ToUpper();
            foreach (SetData set in cardJson.Values) {
                foreach (Card c in set.cards)
                {
                    curDistance = LevenshteinDistance(c.name.ToUpper(), cnu);
                    isSubstringMatch = c.name.ToUpper().Contains(cnu);
                    CardResult test = new CardResult(c, curDistance, isSubstringMatch);
                    if (test.CompareTo(match) < 0)
                    {
                        leastDistance = curDistance;
                        match = test;
                    }
                }
            }
            return match.card;
        }

        public static CardResult[] FuzzyMatch2(Dictionary<string, SetData> cardJson, string cardname, int numMatches)
        {
            int curDistance;
            bool isSubStringMatch;
            List<CardResult> matches = new List<CardResult>();
            string cnu = cardname.ToUpper();
            foreach (SetData set in cardJson.Values)
            {
                foreach (Card c in set.cards)
                {
                    if (!matches.Any(q => q.card.name == c.name)) {
                        curDistance = DamerauLevenshtein(c.name.ToUpper(), cnu);
                        isSubStringMatch = c.name.ToUpper().Contains(cnu);
                        CardResult match = new CardResult(c, curDistance, isSubStringMatch);
                        if (matches.Count == numMatches) {
                            if (match.CompareTo(matches[matches.Count - 1]) < 0)
                            {
                                matches.Add(match);
                                matches.Sort();
                                matches.RemoveAt(numMatches);
                            }
                        } else {
                            matches.Add(match);
                            matches.Sort();
                        }
                    }
                    else
                    {
                        CardResult match = matches.First(q => q.card.name == c.name);
                        matches.Remove(match);
                        curDistance = DamerauLevenshtein(c.name.ToUpper(), cnu);
                        isSubStringMatch = c.name.ToUpper().Contains(cnu);
                        CardResult matchNew = new CardResult(c, curDistance, isSubStringMatch);
                        matches.Add(matchNew);
                        matches.Sort();
                    }
                }
            }
            return matches.ToArray();
        }

        public static int LevenshteinDistance(string s, string t)
        {
            UInt16 n, m;
            n = (UInt16)s.Length;
            m = (UInt16)t.Length;

            if (n == 0)
                return m;
            if (m == 0)
                return n;

            for (UInt16 i = 0; i <= n; d[i, 0] = i++) { }

            for (UInt16 j = 0; j <= m; d[0, j] = j++) { }

            for (int i = 1; i <= n; i++) {
                //Step 4
                for (int j = 1; j <= m; j++)
                {
                    // Step 5
                    int cost = (t[j - 1] == s[i - 1]) ? 0 : 1;

                    // Step 6
                    d[i, j] = (UInt16)Math.Min(
                        Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                        d[i - 1, j - 1] + cost);
                }
            }

            return d[n, m];
        }

        public static int DamerauLevenshtein(string original, string modified)
        {
                int len_orig = original.Length; 
                int len_diff = modified.Length; 

                var matrix = new int[len_orig + 1, len_diff + 1]; 
                for (int i = 0; i <= len_orig; i++) 
                	matrix[i, 0] = i; 
                for (int j = 0; j <= len_diff; j++) 
                	matrix[0, j] = j; 
                 
                for (int i = 1; i <= len_orig; i++) { 
                	for (int j = 1; j <= len_diff; j++) { 
                		int cost = modified[j - 1] == original[i - 1] ? 0 : 1; 
                		var vals = new int[] {
                            matrix[i - 1, j] + 1,
                            matrix[i, j - 1] + 1,
                            matrix[i - 1, j - 1] + cost
                        }; 
                		matrix[i, j] = vals.Min (); 
                		if (i > 1 && j > 1 && original[i - 1] == modified[j - 2] && original[i - 2] == modified[j - 1]) 
                			matrix[i, j] = Math.Min (matrix[i, j], matrix[i - 2, j - 2] + cost); 
                	} 
                } 
                return matrix[len_orig, len_diff];
            }
    }



}
