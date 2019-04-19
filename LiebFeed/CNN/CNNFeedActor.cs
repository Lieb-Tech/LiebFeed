using Akka.Actor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Xml.Linq;

namespace LiebFeed.CNNUS
{
    public class CNNFeedActor : ReceiveActor
    {
        public CNNFeedActor()
        {
            int toProcess = 0;
            int processed = 0;

            IActorRef proc = Context.ActorOf<CNNItemActor>();

            List<string> feeds = new List<string>()
            {
                "http://rss.cnn.com/rss/cnn_us.rss",
                "http://rss.cnn.com/rss/cnn_world.rss",
                "http://rss.cnn.com/rss/cnn_topstories.rss",
                "http://rss.cnn.com/rss/money_latest.rss",
                "http://rss.cnn.com/rss/cnn_allpolitics.rss",
                "http://rss.cnn.com/rss/cnn_latest.rss",
                "http://rss.cnn.com/rss/money_news_companies.rss",
                "http://rss.cnn.com/rss/money_news_international.rss",
                "http://rss.cnn.com/rss/cnn_tech.rss"
            };

            string currentFeed = feeds.First();
            
            Receive<processedCNN>(r =>
            {
                processed++;

                if (processed >= toProcess)
                {
                    if (currentFeed == feeds.Last())
                    {
                        Console.WriteLine("+++CNN Processed");
                        currentFeed = feeds.First();
                        Context.System.Scheduler.ScheduleTellOnce(TimeSpan.FromMinutes(1), Self, new CNNUS.processedCNN(), Self);
                    }
                    else
                    {
                        var cur = feeds.IndexOf(currentFeed);
                        currentFeed = feeds[cur + 1];
                        Self.Tell(new CNNUS.processCNN());
                    }
                }
            });

            Receive<processCNN>(c =>
            {
                toProcess = 0;
                processed = 0;

                string xml = "";

                var feed = currentFeed.Substring(currentFeed.LastIndexOf("/") + 1);
                Console.WriteLine("CNN - Downloading data - " + feed);
                try
                {
                    WebClient wc = new WebClient();
                    xml = wc.DownloadString(currentFeed);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("CNN -- Couldn't download data!!");                    
                }
                if (string.IsNullOrWhiteSpace(xml))
                    Self.Tell(new processedCNN());
                else
                {
                    XDocument xdoc = XDocument.Parse(xml);

                    var items = xdoc.Root.Elements().Elements("item").ToList();
                    toProcess = items.Count();

                    foreach (var item in items)
                    {
                        proc.Tell(new processCNNItem() { item = item });
                    }
                }
            });
        }
    }

    internal class processedCNN
    {
        internal string id;
    }

    internal class processCNN
    {
    }

    internal class processCNNItem
    {
        public XElement item;
    }
}
