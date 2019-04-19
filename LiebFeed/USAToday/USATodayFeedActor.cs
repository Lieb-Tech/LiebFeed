using Akka.Actor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Xml.Linq;

namespace LiebFeed.USAToday
{
    class USATodayFeedActor : ReceiveActor
    {
        public USATodayFeedActor()
        {
            int toProcess = 0;
            int processed = 0;

            IActorRef proc = Context.ActorOf<USATodayItemActor>();

            List<string> feeds = new List<string>()
            {
                "http://rssfeeds.usatoday.com/UsatodaycomNation-TopStories",
                "http://rssfeeds.usatoday.com/UsatodaycomWorld-TopStories",
                "http://rssfeeds.usatoday.com/UsatodaycomWashington-TopStories",
                "http://rssfeeds.usatoday.com/UsatodaycomMoney-TopStories",
                "http://rssfeeds.usatoday.com/usatoday-NewsTopStories"
            };

            string currentFeed = feeds.First();

            Receive<processedUSAToday>(r =>
            {
                processed++;
                if (processed >= toProcess)
                {
                    // Console.WriteLine("     USAToday processed " + currentFeed);
                    if (currentFeed == feeds.Last())
                    {
                        Console.WriteLine("+++USAToday Processed");
                        currentFeed = feeds.First();
                        Context.System.Scheduler.ScheduleTellOnce(TimeSpan.FromMinutes(1), Self, new USAToday.processUSAToday(), Self);
                    }
                    else
                    {
                        var cur = feeds.IndexOf(currentFeed);
                        currentFeed = feeds[cur + 1];
                        Self.Tell(new USAToday.processUSAToday());
                    }
                }
            });

            Receive<processUSAToday>(c =>
            {
                toProcess = 0;
                processed = 0;

                string xml = "";

                var feed = currentFeed.Substring(currentFeed.LastIndexOf("/") + 1);
                Console.WriteLine("USAToday - Downloading data - " + feed);
                try
                {
                    WebClient wc = new WebClient();
                    xml = wc.DownloadString(currentFeed);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("USAToday -- Couldn't download data!!");
                }
                if (string.IsNullOrWhiteSpace(xml))
                    Self.Tell(new processedUSAToday());
                else
                {
                    XDocument xdoc = XDocument.Parse(xml);

                    var items = xdoc.Root.Elements().Elements("item").ToList();
                    toProcess = items.Count();

                    foreach (var item in items)
                    {
                        proc.Tell(new processUSATodayItem() { item = item });
                    }
                }
            });
        }
    }

    internal class processedUSAToday
    {
        public string id { get; internal set; }
    }

    internal class processUSAToday
    {
    }

    internal class processUSATodayItem
    {
        public XElement item;
    }
}
