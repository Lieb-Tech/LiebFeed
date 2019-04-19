using Akka.Actor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Xml.Linq;

namespace LiebFeed.NCMC
{
    public class NCMCFeedActor : ReceiveActor
    {
        public NCMCFeedActor()
        {
            var props = Props.Create<NCMCItemActor>();
            var actor = Context.ActorOf(props, "workers");
            
            int toProcess = 0;
            int processed = 0;

            Context.System.Scheduler.ScheduleTellRepeatedly(TimeSpan.FromMinutes(30),
                        TimeSpan.FromMinutes(30), Self,
                        new processFeedMessage(), Self);
            var active = Context.ActorOf<NCMCActiveActor>();

            List<NCMCActiveItem> activeItems = new List<NCMCActiveItem>();

            Receive<itemProcessed>(e =>
            {
                activeItems.Add(new NCMCActiveItem()
                {
                    age = e.item.age,
                    id = e.item.id,
                    missing = e.item.missing,
                    title = e.item.title,
                    partionKey = e.item.partionKey
                });

                processed++;
                if (processed % 100 == 0)
                    Console.WriteLine(" processed " + processed);

                if (processed == toProcess)
                {
                    Program.cdb.UpsertDocument(new NCMCActive()
                    {
                        active = activeItems
                    }, "ncmc")
                    .Wait();

                    Console.WriteLine("Finished processing NCMC");                    
                }
            });

            Receive<processFeedMessage>(m =>
            {
                var url = $"http://www.missingkids.com/missingkids/servlet/XmlServlet?act=rss&LanguageCountry=en_US&orgPrefix=NCMC";
                string xml = "";

                activeItems.Clear();

                processed = 0;
                toProcess = 0;

                Console.WriteLine("Downloading data - NCMC");
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
                            actor.Tell(new ProcessNCMCItem()
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

    internal class processActive
    {
        public processActive()
        {
        }

        public List<NCMCActive> activeItems  { get; set; }
    }

    internal class ProcessNCMCItem
    {
        public ProcessNCMCItem()
        {
        }

        public XElement item { get; set; }
    }

    internal class processFeedMessage
    {
    }

    internal class itemProcessed
    {
        public NCMCItem item;
    }
}
