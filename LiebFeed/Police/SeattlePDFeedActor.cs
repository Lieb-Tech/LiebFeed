using Akka.Actor;
using LiebFeed.Police.Seattle;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Xml.Linq;

namespace LiebFeed.Police
{
    public class SeattlePDFeedActor : ReceiveActor
    {
        string url = "http://spdblotter.seattle.gov/feed/";
        string url2 = "https://spdblotter.seattle.gov/category/police-precincts/feed/";
        private int toProcess = 0;
        private int processed = 0;

        public SeattlePDFeedActor()
        {
            IActorRef actor = Context.ActorOf<SeattlePDItemActor>();

            Context.System.Scheduler.ScheduleTellRepeatedly(TimeSpan.FromMinutes(30), TimeSpan.FromMinutes(30), Self, new ProcessFeed(), Self);

            Receive<itemProcessed>(p =>
            {
                processed++;
                if (processed == toProcess)
                {
                    Console.WriteLine("Seattle finished processing");
                }
            });

            Receive<ProcessFeed>(f =>
            {
                string xml = "";
                processed = 0;
                toProcess = 0;

                Console.WriteLine("Seattle1 Downloading data - " + url);
                try
                {
                    WebClient wc = new WebClient();
                    xml = wc.DownloadString(url2);
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

                        Console.WriteLine("Seattle elements to process: " + el.Count());

                        toProcess = el.Count();
                        foreach (var e in el)
                        {
                            actor.Tell(new processSPDItem(e));
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Error parsin the data!!");
                    }
                }

                Console.WriteLine("Seattle2 Downloading data - " + url2);
                try
                {
                    WebClient wc = new WebClient();
                    xml = wc.DownloadString(url2);
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

                        Console.WriteLine("Seattle2 elements to process: " + el.Count());

                        toProcess += el.Count();
                        foreach (var e in el)
                        {
                            actor.Tell(new processSPDItem(e));
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

    internal class processSPDItem
    {
        public XElement item;

        public processSPDItem(XElement e)
        {
            this.item = e;
        }
    }

    internal class itemProcessed
    {
        public SeattlePDItem item;
    }

    internal class ProcessFeed
    {
    }
}
