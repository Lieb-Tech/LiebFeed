using Akka.Actor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Xml.Linq;

namespace LiebFeed.NewsFeeds
{
    public class GenericFeedActor : ReceiveActor
    {
        internal class internalFeed
        {
            public string Url;
            public string Feed;
            public string Section;
        }

        public GenericFeedActor()
        {
            int toProcess = 0;
            int processed = 0;
            var proc = Context.ActorOf<GenericItemActor>("generic");
            List<internalFeed> feeds = new List<internalFeed>()
            {
                new internalFeed() { Url = "https://www.dailymail.co.uk/articles.rss", Feed="dailymail", Section="latest"},
                new internalFeed() { Url = "https://www.dailymail.co.uk/ushome/index.rss", Feed="dailymail", Section="u.s."},
                new internalFeed() { Url = "https://www.dailymail.co.uk/news/index.rss", Feed="dailymail", Section="news"},

                new internalFeed() { Url = "https://www.newsweek.com/rss", Feed=  "newsweek", Section = "Latest" },
                new internalFeed() { Url = "http://feeds.nbcnews.com/nbcnews/public/news", Feed=  "nbcnews", Section = "Latest" },
                new internalFeed() { Url = "https://www.huffpost.com/section/front-page/feed", Feed=  "huffpost", Section = "frontPage" },
                new internalFeed() { Url = "https://abcnews.go.com/abcnews/topstories", Feed=  "abc", Section = "topStories" },

                new internalFeed() { Url = "https://www.cbsnews.com/latest/rss/main", Feed=  "cbsnews", Section = "topStories" },
                new internalFeed() { Url = "https://www.cbsnews.com/latest/rss/us", Feed=  "cbsnews", Section = "us" },
                new internalFeed() { Url = "https://www.cbsnews.com/latest/rss/politics", Feed=  "cbsnews", Section = "politics" },
                new internalFeed() { Url = "https://www.cbsnews.com/latest/rss/world", Feed=  "cbsnews", Section = "world" },
                new internalFeed() { Url = "https://www.cbsnews.com/latest/rss/technology", Feed=  "cbsnews", Section = "technology" },

                new internalFeed() { Url = "http://feeds.bbci.co.uk/news/rss.xml",Feed=  "bbc", Section = "latest" },
                new internalFeed() { Url = "http://feeds.bbci.co.uk/news/world/rss.xml",Feed=  "bbc", Section = "world" },
                new internalFeed() { Url = "http://feeds.bbci.co.uk/news/uk/rss.xml",Feed=  "bbc", Section = "uk" },
                new internalFeed() { Url = "http://feeds.bbci.co.uk/news/politics/rss.xml",Feed=  "bbc", Section = "politics" },
                new internalFeed() { Url = "http://feeds.bbci.co.uk/news/technology/rss.xml",Feed=  "bbc", Section = "technology" },
                new internalFeed() { Url = "http://feeds.bbci.co.uk/news/world/us_and_canada/rss.xml",Feed=  "bbc", Section = "usAndCanada" },
            };

            internalFeed currentFeed = feeds.First();

            Receive<processedItem>(r =>
            {
                processed++;

                if (processed >= toProcess)
                {
                    if (currentFeed == feeds.Last())
                    {
                        Console.WriteLine("+++Generic Processed");
                        currentFeed = feeds.First();
                        Context.System.Scheduler.ScheduleTellOnce(TimeSpan.FromMinutes(1), Self, new NewsFeeds.processFeed(), Self);
                    }
                    else
                    {
                        var cur = feeds.IndexOf(currentFeed);
                        currentFeed = feeds[cur + 1];
                        Self.Tell(new NewsFeeds.processFeed());
                    }
                }
            });
            
            Receive<processFeed>(c =>
            {                
                toProcess = 0;
                processed = 0;

                string xml = "";                
                Console.WriteLine(currentFeed.Feed + " - Downloading data - " + currentFeed.Section);
                try
                {
                    WebClient wc = new WebClient();
                    xml = wc.DownloadString(currentFeed.Url);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(currentFeed.Feed + " -- Couldn't download data!!");
                }

                if (string.IsNullOrWhiteSpace(xml))
                    Self.Tell(new processedItem());
                else
                {
                    XDocument xdoc = XDocument.Parse(xml);

                    var items = xdoc.Root.Elements().Elements("item").ToList();
                    toProcess = items.Count();

                    foreach (var item in items)
                    {
                        proc.Tell(new processItem() { item = item, feed = currentFeed.Feed, section = currentFeed.Section });
                    }
                }
            });
        }
    }
}
