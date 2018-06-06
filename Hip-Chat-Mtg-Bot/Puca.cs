using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using System.Web;
using HtmlAgilityPack;

namespace HipchatMTGBot
{
    class Puca
    {
        public static string getPucaPoints(Card card)
        {
            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Referrer = new Uri("https://pucatrade.com/");
            client.DefaultRequestHeaders.Host = "pucatrade.com";
            client.DefaultRequestHeaders.Add("Cookie", "viewedOuibounceModal=true; pucatrade-sess=kh5wRU6VXsqSmsPilTAOT7x1Q5B59i5A9qINFl%2BTwIV4U8EtFfnunuR7P%2BhplZc1eFI5Y71CYi2lQhntx0GzFNeRXllPN5ftFuF51z0a9Xf3Fyrstgbs0jpPh5vavFd51L4i53KLANIGm0V2stqrwzx48NJgZw7YywbI8Zi8G2GJPQY8gAw2OIW0DMZmP9nr8jWAlcfo1uxp%2B7MAqKRhXJDNtP7M0IZGde%2Bcs0%2FE4w1X822zwPwevzWYfEBIQFYsUXLTmPPj4g5Orw6yaWdFdZ%2B%2Fy1XmHsPh%2FR%2BWuUHolW6hKY3Re%2FwwAlXDTakzWhnjQuDkV6A9DGRWGySSbEg3RSuyhjwjkNLCbHpUuieVCMQ5AGakMKL9avuMm8aVPuvBIjB41oMZjp%2Bx5xSTWGej89EFOeBD8fSZDDzcTjd2otz8GjZXl2sA2mzm%2FEH5RGliYaNI2dNpRni2Hgbs9mHvfBNM2zlW9KzoVs2UIe99rRiyGPffYA8YZ4V%2F3FaXfVcF; _ga=GA1.2.1984587840.1498817577; _gid=GA1.2.1226717953.1506004092; __ssid=8274a033-e37e-499c-bd72-9682dfe3c43d; __ar_v4=C4AH3UETZVDY3GO542T7AT%3A20170914%3A44%7CIZ7CRJ5VONC5NPWXZTZU7F%3A20170914%3A44%7CELPGZ5QGR5EYVIC73RYKVT%3A20170914%3A44; __uvt=; uvts=6CxH0gsCypDLZqtC; _gat=1");
            string searchRequest = "https://pucatrade.com/search/2/?cardname=" + HttpUtility.UrlEncode(card.name);

            string originalmessage = client.GetStringAsync(searchRequest).Result;
            string message = originalmessage;
            const string DivString = "<div class=\"price\">";
            const string ActiveTab = "<div class=\"tab tab-1 active\">";
            string cropped = "Pucapoints: ";

            if(message.Contains(ActiveTab))
            {
                message = message.Substring(message.IndexOf(ActiveTab) + ActiveTab.Length);
            }

            while (message.Contains(DivString))
            {
                int length = message.IndexOf('<', message.IndexOf(DivString) + DivString.Length) - (message.IndexOf(DivString) + DivString.Length);

                if(cropped != "Pucapoints: ")
                {
                    cropped += ", (Foil): ";
                }
                cropped += message.Substring(message.IndexOf(DivString) + DivString.Length, length);

                message = message.Substring(message.IndexOf(DivString) + DivString.Length + length);
            }

            return cropped;
        }
    }
}
