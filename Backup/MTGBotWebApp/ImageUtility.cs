using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Drawing;
using TheArtOfDev.HtmlRenderer;
using TheArtOfDev.HtmlRenderer.WinForms;

namespace HipchatMTGBot
{
    class ImageUtility
    {

        internal static string uploadCardImage(SetData set, Card card, Image src)
        {
            string setName = HttpUtility.UrlEncode(set.name).Replace("%", "");

            if (!Directory.Exists("cards/" + setName))
            {
                Directory.CreateDirectory("cards/" + setName);
            }

            string name = HttpUtility.UrlEncode(card.name) + ".jpeg";
            src.Save("cards/" + setName + "/" + name);
            return Program.AzureStorage.Upload(name, setName, "cards");
        }

        internal static string uploadCroppedCardImage(Card card, Image src)
        {
            string cardName = HttpUtility.UrlEncode(card.name.GetHashCode().ToString() + ".jpeg");
            Bitmap target = new Bitmap(170, 130);
            if (src != null)
            {
                using (Graphics g = Graphics.FromImage(target))
                {
                    g.DrawImage(src, new Rectangle(0, 0, target.Width, target.Height),
                                     new Rectangle(28, 40, target.Width, target.Height),
                                     GraphicsUnit.Pixel);
                }
                target.Save("cropped/" + cardName);
            }
            return Program.AzureStorage.Upload(cardName, "cropped", ".");
        }

        internal static string prepareCardImage(SetData set, Card card, bool showCropped = false)
        {
            Image src = null;
            string url = "";

            string setName = HttpUtility.UrlEncode(set.name).Replace("%", "");
            string name = HttpUtility.UrlEncode(card.name) + ".jpeg";
            string filename = "cards/" + setName + "/" + name;

            if (!Directory.Exists("cards/" + setName))
            {
                Directory.CreateDirectory("cards/" + setName);
            }

            try
            {
                if (File.Exists(filename))
                {
                    src = Image.FromFile(filename);
                    url = Program.AzureStorage.IsBlobPresent(name, setName);
                }
            }
            catch (Exception) { }

            if (src == null || url == "")
            {
                try
                {
                    if (src == null)
                    {
                        if (url != "")
                        {
                            try
                            {
                                Program.AzureStorage.Download(setName, name, "cards");
                                if (File.Exists(filename))
                                {
                                    src = Image.FromFile(filename);
                                }
                            }
                            catch (Exception)
                            {
                                src = null;
                            }
                        }

                        if (src == null)
                        {
                            string imageSrc = @"<!DOCTYPE html><html lang=""en"" xmlns=""http://www.w3.org/1999/xhtml""><head><meta charset=""utf-8"" /><title></title></head><body style=""margin:0px;padding:0px;border-radius:5px;background-color:transparent;""><img src=http://gatherer.wizards.com/Handlers/Image.ashx?multiverseid=" + card.multiverseid + @"&amp;type=card style=""margin:0px;padding:0px;border-radius:5px;background-color:transparent;"" width=223 height=311 /></body></html>";
                            src = TheArtOfDev.HtmlRenderer.WinForms.HtmlRender.RenderToImage(imageSrc);
                        }
                    }

                    if (url == "")
                    {
                        url = uploadCardImage(set, card, src);
                    }
                }
                catch (Exception) { }
            }

            if (showCropped == true)
            {
                try
                {
                    string cardName = HttpUtility.UrlEncode(card.name.GetHashCode().ToString() + ".jpeg");
                    url = Program.AzureStorage.IsBlobPresent(cardName, "cropped");
                    if (url.Equals("") || File.Exists("cards/" + setName + "/" + HttpUtility.UrlEncode(card.name) + ".jpeg") || Program.AzureStorage.Download(setName, name, "cards"))
                    {
                        src = Image.FromFile("cards/" + setName + "/" + HttpUtility.UrlEncode(card.name) + ".jpeg");
                        url = uploadCroppedCardImage(card, src);
                    }
                }
                catch (Exception)
                { }
            }

            return url;
        }

    }
}
