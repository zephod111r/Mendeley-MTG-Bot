using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace HipchatMTGBot
{
    class MTGSymbols
    {
        static private Dictionary<string, string> symbolReplacement = new Dictionary<string, string>()
        {
            { "{C}",  "<img alt='{C}' src='http://gatherer.wizards.com/Handlers/Image.ashx?size=small&name=C&type=symbol' width='15px' height='15px' />" },
            { "{∞}",  "<img alt='{∞}' src='http://gatherer.wizards.com/Handlers/Image.ashx?size=small&name=Infinity&type=symbol' width='15px' height='15px' />" },
            { "{½}",  "<img alt='{½}' src='http://gatherer.wizards.com/Handlers/Image.ashx?size=small&name=Half&type=symbol' width='15px' height='15px' />" },
            { "{S}",  "<img alt='{S}' src='http://gatherer.wizards.com/Handlers/Image.ashx?size=small&name=Snow&type=symbol' width='15px' height='15px' />" },
            { "{E}",  "<img alt='{E}' src='http://gatherer.wizards.com/Handlers/Image.ashx?size=small&name=E&type=symbol' width='15px' height='15px' />" },
            { "{0}",  "<img alt='{0}' src='http://gatherer.wizards.com/Handlers/Image.ashx?size=small&name=0&type=symbol' width='15px' height='15px' />" },
            { "{1}",  "<img alt='{1}' src='http://gatherer.wizards.com/Handlers/Image.ashx?size=small&name=1&type=symbol' width='15px' height='15px' />" },
            { "{2}",  "<img alt='{2}' src='http://gatherer.wizards.com/Handlers/Image.ashx?size=small&name=2&type=symbol' width='15px' height='15px' />" },
            { "{3}",  "<img alt='{3}' src='http://gatherer.wizards.com/Handlers/Image.ashx?size=small&name=3&type=symbol' width='15px' height='15px' />" },
            { "{4}",  "<img alt='{4}' src='http://gatherer.wizards.com/Handlers/Image.ashx?size=small&name=4&type=symbol' width='15px' height='15px' />" },
            { "{5}",  "<img alt='{5}' src='http://gatherer.wizards.com/Handlers/Image.ashx?size=small&name=5&type=symbol' width='15px' height='15px' />" },
            { "{6}",  "<img alt='{6}' src='http://gatherer.wizards.com/Handlers/Image.ashx?size=small&name=6&type=symbol' width='15px' height='15px' />" },
            { "{7}",  "<img alt='{7}' src='http://gatherer.wizards.com/Handlers/Image.ashx?size=small&name=7&type=symbol' width='15px' height='15px' />" },
            { "{8}",  "<img alt='{8}' src='http://gatherer.wizards.com/Handlers/Image.ashx?size=small&name=8&type=symbol' width='15px' height='15px' />" },
            { "{9}",  "<img alt='{9}' src='http://gatherer.wizards.com/Handlers/Image.ashx?size=small&name=9&type=symbol' width='15px' height='15px' />" },
            { "{10}", "<img alt='{10}' src='http://gatherer.wizards.com/Handlers/Image.ashx?size=small&name=10&type=symbol' width='15px' height='15px' />" },
            { "{11}", "<img alt='{11}' src='http://gatherer.wizards.com/Handlers/Image.ashx?size=small&name=11&type=symbol' width='15px' height='15px' />" },
            { "{12}", "<img alt='{12}' src='http://gatherer.wizards.com/Handlers/Image.ashx?size=small&name=12&type=symbol' width='15px' height='15px' />" },
            { "{13}", "<img alt='{13}' src='http://gatherer.wizards.com/Handlers/Image.ashx?size=small&name=13&type=symbol' width='15px' height='15px' />" },
            { "{14}", "<img alt='{14}' src='http://gatherer.wizards.com/Handlers/Image.ashx?size=small&name=14&type=symbol' width='15px' height='15px' />" },
            { "{15}", "<img alt='{15}' src='http://gatherer.wizards.com/Handlers/Image.ashx?size=small&name=15&type=symbol' width='15px' height='15px' />" },
            { "{16}", "<img alt='{16}' src='http://gatherer.wizards.com/Handlers/Image.ashx?size=small&name=16&type=symbol' width='15px' height='15px' />" },
            { "{100}", "<img alt='{100}' src='http://gatherer.wizards.com/Handlers/Image.ashx?size=small&name=100&type=symbol' width='15px' height='15px' />" },
            { "{X}",  "<img alt='{X}' src='http://gatherer.wizards.com/Handlers/Image.ashx?size=small&name=X&type=symbol' width='15px' height='15px' />" },
            { "{W}",  "<img alt='{W}' src='http://gatherer.wizards.com/Handlers/Image.ashx?size=small&name=W&type=symbol' width='15px' height='15px' />" },
            { "{U}",  "<img alt='{U}' src='http://gatherer.wizards.com/Handlers/Image.ashx?size=small&name=U&type=symbol' width='15px' height='15px' />" },
            { "{B}",  "<img alt='{B}' src='http://gatherer.wizards.com/Handlers/Image.ashx?size=small&name=B&type=symbol' width='15px' height='15px' />" },
            { "{R}",  "<img alt='{R}' src='http://gatherer.wizards.com/Handlers/Image.ashx?size=small&name=R&type=symbol' width='15px' height='15px' />" },
            { "{hr}",  "<img alt='{hr}' src='http://gatherer.wizards.com/Handlers/Image.ashx?size=small&name=HalfR&type=symbol' width='8px' height='15px' />" },
            { "{G}",  "<img alt='{G}' src='http://gatherer.wizards.com/Handlers/Image.ashx?size=small&name=G&type=symbol' width='15px' height='15px' />" },
            { "{R/G}", "<img alt='{R/G}' src='http://gatherer.wizards.com/Handlers/Image.ashx?size=small&name=RG&type=symbol' width='15px' height='15px' />" },
            { "{W/U}", "<img alt='{W/U}' src='http://gatherer.wizards.com/Handlers/Image.ashx?size=small&name=WU&type=symbol' width='15px' height='15px' />" },
            { "{U/R}", "<img alt='{U/R}' src='http://gatherer.wizards.com/Handlers/Image.ashx?size=small&name=UR&type=symbol' width='15px' height='15px' />" },
            { "{U/B}", "<img alt='{U/B}' src='http://gatherer.wizards.com/Handlers/Image.ashx?size=small&name=UB&type=symbol' width='15px' height='15px' />" },
            { "{B/R}", "<img alt='{B/R}' src='http://gatherer.wizards.com/Handlers/Image.ashx?size=small&name=BR&type=symbol' width='15px' height='15px' />" },
            { "{B/G}", "<img alt='{B/G}' src='http://gatherer.wizards.com/Handlers/Image.ashx?size=small&name=BG&type=symbol' width='15px' height='15px' />" },
            { "{G/U}", "<img alt='{G/U}' src='http://gatherer.wizards.com/Handlers/Image.ashx?size=small&name=GU&type=symbol' width='15px' height='15px' />" },
            { "{G/W}", "<img alt='{G/W}' src='http://gatherer.wizards.com/Handlers/Image.ashx?size=small&name=GW&type=symbol' width='15px' height='15px' />" },
            { "{R/W}", "<img alt='{R/W}' src='http://gatherer.wizards.com/Handlers/Image.ashx?size=small&name=RW&type=symbol' width='15px' height='15px' />" },
            { "{W/B}", "<img alt='{W/B}' src='http://gatherer.wizards.com/Handlers/Image.ashx?size=small&name=WB&type=symbol' width='15px' height='15px' />" },
            { "{2/W}", "<img alt='{2/W}' src='http://gatherer.wizards.com/Handlers/Image.ashx?size=small&name=2W&type=symbol' width='15px' height='15px' />" },
            { "{2/U}", "<img alt='{2/U}' src='http://gatherer.wizards.com/Handlers/Image.ashx?size=small&name=2U&type=symbol' width='15px' height='15px' />" },
            { "{2/B}", "<img alt='{2/B}' src='http://gatherer.wizards.com/Handlers/Image.ashx?size=small&name=2B&type=symbol' width='15px' height='15px' />" },
            { "{2/R}", "<img alt='{2/R}' src='http://gatherer.wizards.com/Handlers/Image.ashx?size=small&name=2R&type=symbol' width='15px' height='15px' />" },
            { "{2/G}", "<img alt='{2/G}' src='http://gatherer.wizards.com/Handlers/Image.ashx?size=small&name=2G&type=symbol' width='15px' height='15px' />" },
            { "{G/P}", "<img alt='{G/P}' src='http://gatherer.wizards.com/Handlers/Image.ashx?size=small&name=GP&type=symbol' width='15px' height='15px' />" },
            { "{R/P}", "<img alt='{R/P}' src='http://gatherer.wizards.com/Handlers/Image.ashx?size=small&name=RP&type=symbol' width='15px' height='15px' />" },
            { "{B/P}", "<img alt='{B/P}' src='http://gatherer.wizards.com/Handlers/Image.ashx?size=small&name=BP&type=symbol' width='15px' height='15px' />" },
            { "{W/P}", "<img alt='{W/P}' src='http://gatherer.wizards.com/Handlers/Image.ashx?size=small&name=WP&type=symbol' width='15px' height='15px' />" },
            { "{U/P}", "<img alt='{U/P}' src='http://gatherer.wizards.com/Handlers/Image.ashx?size=small&name=UP&type=symbol' width='15px' height='15px' />" },
            { "{T}",  "<img alt='{T}' src='http://gatherer.wizards.com/Handlers/Image.ashx?size=small&name=tap&type=symbol' width='15px' height='15px' />" },
            { "{Q}",  "<img alt='{Q}' src='http://gatherer.wizards.com/Handlers/Image.ashx?size=small&name=untap&type=symbol' width='15px' height='15px' />" }
        };

        const string regexPatternManaOrTapSymbol = @"{[^{}]+}";

        internal static string convertToHtmlSymbols(string inputStr)
        {
            List<string> ignoreList = new List<string>();

            string input = inputStr;

            if (input == null) return "";

            foreach (Match match in Regex.Matches(input, regexPatternManaOrTapSymbol))
            {
                string value = match.Value;

                if (ignoreList.Contains(value))
                {
                    continue;
                }
                ignoreList.Add(value);

                string switchSymbol = match.Value;
                symbolReplacement.TryGetValue(value, out switchSymbol);
                input = input.Replace(match.Value, switchSymbol);
            }

            return input;
        }
    }
}
