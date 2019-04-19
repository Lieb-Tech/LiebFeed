using Akka.Actor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Xml.Linq;

namespace LiebFeed.FoxNews
{
    public class FoxFeedActor : ReceiveActor
    {
        public FoxFeedActor()
        {
            int toProcess = 0;
            int processed = 0;

            IActorRef proc = Context.ActorOf<FoxItemActor>();

            List<string> feeds = new List<string>()
            {
                "http://feeds.foxnews.com/foxnews/latest",
                "http://feeds.foxnews.com/foxnews/national",
                "http://feeds.foxnews.com/foxnews/world",
                "http://feeds.foxnews.com/foxnews/politics",
                "http://feeds.foxnews.com/foxnews/business",
                "http://feeds.foxnews.com/foxnews/scitech"
            };

            string currentFeed = feeds.First();

            Receive<processedFox>(r =>
            {
                processed++;                
                if (processed >= toProcess)
                {
                    // Console.WriteLine("     FOX processed " + currentFeed);
                    if (currentFeed == feeds.Last())
                    {
                        Console.WriteLine("+++FOX Processed");
                        currentFeed = feeds.First();
                        Context.System.Scheduler.ScheduleTellOnce(TimeSpan.FromMinutes(1), Self, new FoxNews.processedFox(), Self);
                    }
                    else
                    {
                        var cur = feeds.IndexOf(currentFeed);
                        currentFeed = feeds[cur + 1];
                        Self.Tell(new FoxNews.processFox());
                    }
                }
            });

            Receive<processFox>(c =>
            {
                toProcess = 0;
                processed = 0;

                string xml = "";

                var feed = currentFeed.Substring(currentFeed.LastIndexOf("/") + 1);
                Console.WriteLine("Fox - Downloading data - " + feed);
                try
                {
                    WebClient wc = new WebClient();
                    xml = wc.DownloadString(currentFeed);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Fox -- Couldn't download data!!");                    
                }
                if (string.IsNullOrWhiteSpace(xml))
                    Self.Tell(new processedFox());
                else
                {
                    XDocument xdoc = XDocument.Parse(xml);

                    var items = xdoc.Root.Elements().Elements("item").ToList();
                    toProcess = items.Count();

                    foreach (var item in items)
                    {
                        proc.Tell(new processFoxItem() { item = item });
                    }
                }
            });
        }
    }

    internal class processedFox
    {
        public string id { get; internal set; }
    }

    internal class processFox
    {
    }

    internal class processFoxItem
    {
        public XElement item;
    }
}
