using Akka.Actor;
using Akka.Routing;
using LiebFeed.USGS.FeedDataStructures;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Xml.Linq;

namespace LiebFeed.USGS
{
    public class USGSFeedActor : ReceiveActor
    {
        bool initial = true;
        public USGSFeedActor()
        {
            //actor = Context.ActorOf<USGSItemActor>();
            var props = Props.Create<USGSItemActor>().WithRouter(new RoundRobinPool(5));
            var actor = Context.ActorOf(props, "workers");
            var recent = Context.ActorOf<USGSRecentActor>();            

            Receive<processRecent>(z =>
            {
                recent.Tell(z);
            });

            int toProcess = 0;
            int processed = 0;
            Receive<itemProcessed>(e =>
                {
                    processed++;

                    if (processed % 100 == 0)
                        Console.WriteLine(" processed " + processed);

                    if (processed == toProcess)
                    {
                        Console.WriteLine("++USGS Finished processing");
                        if (initial)
                        {
                            initial = false;
                            Context.System.Scheduler.ScheduleTellRepeatedly(TimeSpan.FromMinutes(1),
                            TimeSpan.FromMinutes(1), Self,
                            new processFeedMessage()
                            {
                                path = "all_hour.atom"
                            }, Self);
                        }
                    }
                });

            Receive<processFeedMessage>(m =>
            {
                var url = $"https://earthquake.usgs.gov/earthquakes/feed/v1.0/summary/{m.path}";
                string xml = "";

                processed = 0;
                toProcess = 0;

                Console.WriteLine("Downloading data - " + m.path);
                try
                {
                    WebClient wc = new WebClient();
                    xml = wc.DownloadString(url);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Couldn't download data!!");
                }

                if (!string.IsNullOrWhiteSpace(xml))
                {
                    try
                    {
                        XDocument xdoc = XDocument.Parse(xml);
                        var el = xdoc.Root.Elements().Where(z => z.Name.LocalName == "entry").ToList();

                        Console.WriteLine("Elements to process: " + el.Count());
                        toProcess += el.Count();
                        foreach (var e in el)
                        {
                            actor.Tell(new ProcessUSGSItem()
                            {
                                item = e,
                                path = m.path
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Error parsin the data!!");
                    }
                }
            });
        }
    }

    internal class itemProcessed
    {
    }

    internal class processFeedMessage
    {
        public string path;
    }
}
