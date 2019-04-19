using Akka.Actor;
using Akka.Routing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Xml.Linq;

namespace LiebFeed.SpotCrime
{
    public class SpotCrimeFeedActor : ReceiveActor
    {
        public SpotCrimeFeedActor()
        {
            /// var url = "http://s3.spotcrime.com/cache/rss/fl-volusia-county.xml";
            int processed = 0;
            int toProcess = 0;
            int newItems = 0;

            string feed = "";
            var props = Props.Create<SpotCrimeItemActor>().WithRouter(new RoundRobinPool(5));
            var actor = Context.ActorOf(props, "workers");            

            Receive<processedCSItem>(r =>
            {
                processed++;
                if (r.newItem)
                    newItems++;
                if (processed == toProcess)
                {
                    Console.WriteLine("__SpotCrime finished -- " + feed);
                    
                    Program.cdb.UpsertDocument(new
                    {
                        id = "recent:" + feed,
                        partionKey = "recent",
                        feed = feed,
                        totalItems = toProcess,
                        newItems = newItems
                    }, "spotcrime").Wait();
                }
            });

            Receive<noneToProcess>(r =>
            {
                processed++;
                    Console.WriteLine("__SpotCrime finished -- " + feed);

                    Program.cdb.UpsertDocument(new
                    {
                        id = "recent:" + feed,
                        partionKey = "recent",
                        feed = feed,
                        totalItems = toProcess,
                        newItems = newItems
                    }, "spotcrime").Wait();                
            });

            Receive<processSCFeed>(r =>
            {
                string xml = "";
                newItems = 0;
                processed = 0;
                toProcess = 0;

                feed = r.url.Substring(r.url.LastIndexOf("/") + 1);
                feed = feed.Substring(0, feed.Length - 4);
                Console.WriteLine("..Downloading data - SpotCrime - " + feed);
                try
                {
                    WebClient wc = new WebClient();
                    xml = wc.DownloadString(r.url);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Couldn't download data -- SpotCrime!! " + feed);
                }

                if (!string.IsNullOrWhiteSpace(xml))
                {
                    try
                    {
                        XDocument xdoc = XDocument.Parse(xml);
                        var el = xdoc.Root.Elements().Elements("item").ToList();

                        Console.WriteLine(".. " + feed + " - SpotCrime elements to process: " + el.Count());
                        toProcess += el.Count();
                        foreach (var e in el)
                        {
                            actor.Tell(new processSCItem() {
                                 item = e,
                                 location = feed
                            });
                        }

                        if (toProcess == 0)
                            Self.Tell(new noneToProcess());
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Error parsin the data -- SpotCrime");
                    }
                }
            });
        }
    }

    internal class noneToProcess
    {
    }

    internal class processSCItem
    {
        public processSCItem()
        {
        }
        public string location { get; set; }
        public XElement item { get; set; }
    }

    internal class processSCFeed
    {
        internal string url;
    }

    internal class processedCSItem
    {
        internal string location;
        internal bool newItem;
    }
}
