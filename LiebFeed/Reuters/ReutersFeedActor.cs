using Akka.Actor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Xml.Linq;

namespace LiebFeed.Reuters
{
    class ReutersFeedActor : ReceiveActor
    {
        public ReutersFeedActor()
        {            
            int toProcess = 0;
            int processed = 0;

            IActorRef proc = Context.ActorOf<ReutersItemActor>();

            List<string> feeds = new List<string>()
            {
                "http://feeds.reuters.com/reuters/topNews",
                "http://feeds.reuters.com/Reuters/domesticNews",
                "http://feeds.reuters.com/Reuters/worldNews",
                "http://feeds.reuters.com/reuters/businessNews",
                "http://feeds.reuters.com/reuters/companyNews",
                "http://feeds.reuters.com/Reuters/PoliticsNews", 
                "http://feeds.reuters.com/reuters/scienceNews",
                "http://feeds.reuters.com/reuters/technologyNews"
            };

            string currentFeed = feeds.First();

            Receive<processedReuters>(r =>
            {
                processed++;
                if (processed >= toProcess)  
                {
                    if (currentFeed == feeds.Last())
                    {
                        Console.WriteLine("+++Reuters Processed");
                        currentFeed = feeds.First();
                        Context.System.Scheduler.ScheduleTellOnce(TimeSpan.FromMinutes(1), Self, new Reuters.processedReuters(), Self);
                    }
                    else
                    {
                        var cur = feeds.IndexOf(currentFeed);
                        currentFeed = feeds[cur + 1];
                        Self.Tell(new Reuters.processReuters());
                    }
                }
                    
            });

            Receive<processReuters>(c =>
            {
                toProcess = 0;
                processed = 0;

                string xml = "";

                var feed = currentFeed.Substring(currentFeed.LastIndexOf("/") + 1);
                Console.WriteLine("Reuters - Downloading data - " + feed);
                try
                {
                    WebClient wc = new WebClient();
                    xml = wc.DownloadString(currentFeed);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Reuters -- Couldn't download data!!");
                }

                if (string.IsNullOrWhiteSpace(xml))
                    Self.Tell(new processedReuters());
                else
                {
                    XDocument xdoc = XDocument.Parse(xml);

                    var items = xdoc.Root.Elements().Elements("item").ToList();
                    toProcess = items.Count();

                    foreach (var item in items)
                    {
                        proc.Tell(new processReutersItem() { item = item });
                    }
                }
            });
        }
    }

    internal class processedReuters
    {
        internal string id;
    }

    internal class processReuters
    {
    }

    internal class processReutersItem
    {
        public XElement item;
    }
}
