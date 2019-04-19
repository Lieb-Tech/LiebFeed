using Akka.Actor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Xml.Linq;

namespace LiebFeed.InciWeb
{
    public class InciWebFeedActor : ReceiveActor
    {
        public InciWebFeedActor()
        {
            var url = "https://inciweb.nwcg.gov/feeds/rss/incidents/";

            var actor = Context.ActorOf<InciWebItemActor>();
            // var active = Context.ActorOf<InciWebActiveActor>();

            int processed = 0;
            int toProcess = 0;

            var activeItems = new List<InciWebActiveItem>();

            Context.System.Scheduler.ScheduleTellRepeatedly(TimeSpan.FromSeconds(30), TimeSpan.FromHours(1), Self, new processInciWeb(), Self);

            Receive<processedInciWeb>(r =>
            {
                // active.Tell(r);
                activeItems.Add(new InciWebActiveItem()
                {
                    id = r.item.id,
                    partionKey = r.item.partionKey,
                    pubDate = r.item.pubDate,
                    title = r.item.title
                });

                processed++;
                if (processed == toProcess)
                {
                    // remove old Actives
                    Console.WriteLine("Finished InciWeb");

                    var active = new InciWebActive()
                    {
                        id = "active",
                        partionKey = "active",
                        updates = activeItems
                    };
                    Program.cdb.UpsertDocument(active, "inciweb").Wait();                    
                }
            });            

            Receive<processInciWeb>(r =>
            {                
                string xml = "";
                processed = 0;
                toProcess = 0;

                activeItems.Clear();

                Console.WriteLine("Downloading data - " + url);
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
                        var el = xdoc.Root.Elements().Elements().Where(z => z.Name.LocalName == "item").ToList();

                        Console.WriteLine("Elements to process: " + el.Count());

                        toProcess = el.Count();
                        foreach (var e in el)
                        {
                            actor.Tell(new processInciWebItem(e));
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

    internal class ensureActive
    {
        public InciWebItem item;
    }

    internal class processedInciWeb
    {
        public InciWebItem item;
    }

    internal class processInciWeb
    {
    }
}
