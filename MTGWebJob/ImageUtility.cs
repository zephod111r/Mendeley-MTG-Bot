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

            System.IO.Stream stream = new System.IO.MemoryStream();
            src.Save(stream, System.Drawing.Imaging.ImageFormat.Jpeg);

            return Program.AzureStorage.Upload(stream, name, setName);
        }

        internal static string uploadCroppedCardImage(Card card, Image src)
        {
            string cardName = HttpUtility.UrlEncode(card.name.GetHashCode().ToString() + ".jpeg");
            Bitmap target = new Bitmap(170, 130);
            using (System.IO.Stream stream = new System.IO.MemoryStream())
            {
                using (Graphics g = Graphics.FromImage(target))
                {
                    g.DrawImage(src, new Rectangle(0, 0, target.Width, target.Height),
                                        new Rectangle(28, 40, target.Width, target.Height),
                                        GraphicsUnit.Pixel);
                }
                target.Save(stream, System.Drawing.Imaging.ImageFormat.Jpeg);
                stream.Seek(0, SeekOrigin.Begin);
                return Program.AzureStorage.Upload(stream, cardName, "cropped");
            }
        }

        internal static string prepareCardImage(SetData set, Card card, bool showCropped = false)
        {
            string url = "";

            string setName = HttpUtility.UrlEncode(set.name).Replace("%", "");
            string name = HttpUtility.UrlEncode(card.name) + ".jpeg";
            
            try
            {
                url = Program.AzureStorage.IsBlobPresent(name, setName);
            }
            catch (Exception) { }

            if (url == "")
            {
                try
                {
                    string imageSrc = @"<!DOCTYPE html><html lang=""en"" xmlns=""http://www.w3.org/1999/xhtml""><head><meta charset=""utf-8"" /><title></title></head><body style=""margin:0px;padding:0px;border-radius:5px;background-color:transparent;""><img src=http://gatherer.wizards.com/Handlers/Image.ashx?multiverseid=" + card.multiverseid + @"&amp;type=card style=""margin:0px;padding:0px;border-radius:5px;background-color:transparent;"" width=223 height=311 /></body></html>";
                    using (Image src = TheArtOfDev.HtmlRenderer.WinForms.HtmlRender.RenderToImage(imageSrc))
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
                    if (url.Equals(""))
                    {
                        Stream stream = Program.AzureStorage.Download(setName, name, "cards");
                        using (Image src = Image.FromStream(stream))
                        {
                            url = uploadCroppedCardImage(card, src);
                        }
                    }
                }
                catch (Exception)
                { }
            }

            return url;
        }

    }
}
