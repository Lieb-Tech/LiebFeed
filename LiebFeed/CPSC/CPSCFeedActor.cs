using Akka.Actor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Xml.Linq;

namespace LiebFeed.CSPC
{
    public class CPSCFeedActor : ReceiveActor
    {
        public CPSCFeedActor()
        {
            var url = "https://www.cpsc.gov/Newsroom/CPSC-RSS-Feed/Recalls-RSS";

            var props = Props.Create<CPSCItemActor>();
            var actor = Context.ActorOf(props, "workers");

            int toProcess = 0;
            int processed = 0;

            List<CPSCActiveItem> activeItems = new List<CPSCActiveItem>();

            Context.System.Scheduler.ScheduleTellRepeatedly(TimeSpan.FromMinutes(1),
                        TimeSpan.FromHours(1), Self,
                        new processFeedMessage(), Self);
            
            Receive<processedCSPC>(e =>
            {
                activeItems.Add(new CPSCActiveItem()
                {
                    id = e.item.id,
                    title = e.item.title,
                    link = e.item.link,
                    partionKey = e.item.partionKey
                });

                processed++;
                if (processed % 100 == 0)
                    Console.WriteLine(" processed " + processed);

                if (processed == toProcess)
                {
                    Program.cdb.UpsertDocument(new CPSCActive()
                    {
                        active = activeItems
                    }, "cpsc")
                    .Wait();

                    Console.WriteLine("Finished processing CPSC");
                }
            });

            Receive<processFeedMessage>(m =>
            {
                string xml = "";

                processed = 0;
                toProcess = 0;

                activeItems.Clear();

                Console.WriteLine("Downloading data - CPSPC");
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
                        toProcess += el.Count();
                        foreach (var e in el)
                        {
                            actor.Tell(new ProcessCPSCItem()
                            {
                                item = e,
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

    internal class ProcessCPSCItem
    {
        public ProcessCPSCItem()
        {
        }

        public XElement item { get; set; }
    }

    internal class processFeedMessage
    {
    }

    internal class processedCSPC
    {
        public CPSCItem item;
    }
}
