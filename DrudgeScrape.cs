using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.IO;

namespace DrudgeToReddit
{
    class DrudgeScrape
    {
        private const string DRUDGE_URL = @"http://drudgereport.com";

        public readonly List<Link> Links = new List<Link>();

        public DrudgeScrape()
        {
            var request = WebRequest.Create(DRUDGE_URL);
            var response = request.GetResponse();
            var stream = response.GetResponseStream();
            var responseBody = (new StreamReader(stream)).ReadToEnd();
            var htmlParsed = new HtmlAgilityPack.HtmlDocument();
            htmlParsed.LoadHtml(responseBody);
            var links = htmlParsed.DocumentNode.SelectNodes("//a");

            foreach(var link in links)
            {
                var url = link.GetAttributeValue("href", string.Empty);
                var text = link.InnerText;
                if (url != string.Empty && text != string.Empty)
                {
                    Links.Add(new Link() { Url = url, Text = text });
                }
            }
        }
    }
}
