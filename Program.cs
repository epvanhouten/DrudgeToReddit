using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using System.IO;
using RedditSharp;

namespace DrudgeToReddit
{
    class Program
    {
        private const string HISTORY_PATH = @"history.xml";
        private const string CREDENTIAL_PATH = @"credentials.xml";
        private const string TEST_PATH = @"test.xml";

        private static Subreddit _drudgeSubreddit;
        private static Reddit _reddit;
        private static AuthenticatedUser _user;

        static void Main(string[] args)
        {
            InitilizeReddit();
            
            var serializer = new XmlSerializer(typeof(List<Link>));

            #region Test code, does not execute during normal operation
            if (File.Exists(TEST_PATH))
            {

                var testSet = (List<Link>)serializer.Deserialize(new FileStream(TEST_PATH, FileMode.Open));
                foreach (var link in testSet.Take(1))
                {
                    SubmitNewLink(link);
                }
                return;
            }
            #endregion

            var scrape = new DrudgeScrape();

            if(File.Exists(HISTORY_PATH))
            {
                var history = GetHistoryFromFile(serializer);

                var workToDo = CompareLinksToHistory(scrape, history);

                foreach (var link in workToDo.NewLinks)
                {
                    SubmitNewLink(link);
                }

                WriteHistory(workToDo.History, serializer);
                ClearModerationQueue();
            }
            else
            {
                WriteHistory(scrape.Links, serializer);
            }
        }

        private static void ClearModerationQueue()
        {
            var moderationItems = _drudgeSubreddit.GetModQueue();
            foreach (VotableThing thing in moderationItems)
            {
                var post = thing as Post;
                if (post == null) continue;
                if (post.Author.FullName == _reddit.User.FullName)
                {
                    post.Approve();
                }
            }
        }

        private static List<Link> GetHistoryFromFile(XmlSerializer serializer)
        {
            List<Link> history;
            using (var fileStream = new FileStream(HISTORY_PATH, FileMode.Open))
            {
                history = (List<Link>)serializer.Deserialize(fileStream);
            }
            return history;
        }

        private static HistoryComparisonResult CompareLinksToHistory(DrudgeScrape scrape, List<Link> history)
        {
            var result = new HistoryComparisonResult();

            foreach (var link in scrape.Links)
            {
                result.History.Add(link);

                if (!history.Contains(link))
                {
                    result.NewLinks.Add(link);
                }
            }

            //Copy over any history items that weren't still on the page or aged out
            foreach (var historyItem in history.Where(x => x.ObservedTime > DateTime.Now.AddDays(-2))
                                               .Where(x => !result.History.Contains(x)))
            {
                result.History.Add(historyItem);
            }

            return result;
        }

        private class HistoryComparisonResult
        {
            public List<Link> NewLinks = new List<Link>();
            public List<Link> History = new List<Link>();
        }

        private static void InitilizeReddit()
        {
            if (!File.Exists(CREDENTIAL_PATH)) throw new FileNotFoundException("Creditial File Not Found", CREDENTIAL_PATH);

            var stream = new FileStream(CREDENTIAL_PATH, FileMode.Open);
            var serializer = new XmlSerializer(typeof(Credentials));
            var creds = (Credentials)serializer.Deserialize(stream);
            _reddit = new Reddit();
            WebAgent.UserAgent = "DrudgeReport Scraper";
            _user = _reddit.LogIn(creds.UserName, creds.Password);
            _drudgeSubreddit = _reddit.GetSubreddit(@"drudgereport");
        }

        private static void WriteHistory(List<Link> links, XmlSerializer serializer)
        {
            using (var fileStream = new FileStream(HISTORY_PATH, FileMode.Create))
            {
                serializer.Serialize(fileStream, links);
            }
        }

        private static void SubmitNewLink(Link linkToSubmit)
        {
            try
            {
                _drudgeSubreddit.SubmitPost(linkToSubmit.Text, linkToSubmit.Url);
            }
            catch (Exception e)
            {
                using (var stream = new FileStream("errors.txt", FileMode.Append))
                {
                    var writer = new StreamWriter(stream);
                    writer.WriteLine(e.Message);
                }
            }
        }
    }
}
