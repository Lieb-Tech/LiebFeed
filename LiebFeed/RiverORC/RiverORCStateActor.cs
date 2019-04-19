using Akka.Actor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Xml.Linq;

namespace LiebFeed.RiverORC
{
    public class RiverORCStateActor : ReceiveActor
    {
        public RiverORCStateActor()
        {
            int toProcess = 0;
            int processed = 0;
            string state = "";

            var props = Props.Create<RiverORCItemActor>();
            var actor = Context.ActorOf(props, "riverItem");

            Receive<processedORC>(z =>
            {
                processed++;
                if (processed == toProcess)
                {
                    Console.WriteLine("..." + state.ToUpper() + " Finished ");                    
                    Context.Parent.Tell(new processedORC());
                }
                else if (processed % 50 == 0)
                    Console.WriteLine(".........." + state.ToUpper() + " --- procssed:" + processed);
            });
            Receive<processStateMessage>(s =>
            {
                var urlBase = "https://water.weather.gov/ahps2/rss/obs/{0}.rss";
                var url = string.Format(urlBase, s.state);

                // url = "https://water.weather.gov/ahps2/hydrograph.php?wfo=mob&amp;gage=yela1";

                state = s.state;
                toProcess = 0;
                processed = 0;

                string xml = "";

                Console.WriteLine("Downloading data - RiverORC - " + s.state);
                try
                {
                    WebClient wc = new WebClient();
                    wc.Headers.Add(HttpRequestHeader.Host, "water.weather.gov");
                    wc.Headers.Add(HttpRequestHeader.Pragma, "no-cache");
                    wc.Headers.Add(HttpRequestHeader.CacheControl, "no-cache");
                    wc.Headers.Add(HttpRequestHeader.UserAgent, "Win10");
                    wc.Headers.Add(HttpRequestHeader.Accept, "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,image/apng,*/*;q=0.8");

                    xml = wc.DownloadString(url);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Couldn't download data!!");
                    Context.Parent.Tell(new processedORC());
                }

                if (!string.IsNullOrWhiteSpace(xml))
                {
                    try
                    {
                        XDocument xdoc = XDocument.Parse(xml);
                        var el = xdoc.Root.Elements().First().Elements("item").ToList();

                        Console.WriteLine("Elements to process: " + el.Count());
                        toProcess += el.Count();
                        foreach (var e in el)
                        {

                            actor.Tell(new ProcessORCItem()
                            {
                                state = s.state,
                                item = e,
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Error parsin the data!!");
                        Context.Parent.Tell(new processedORC());
                    }
                }
            });            
        }
    }
}
