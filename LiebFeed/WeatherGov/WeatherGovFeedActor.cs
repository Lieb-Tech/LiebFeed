using Akka.Actor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Xml.Linq;

namespace LiebFeed.WeatherGov
{
    public class WeatherGovFeedActor : ReceiveActor
    {
        public WeatherGovFeedActor()
        {
            var url = "https://alerts.weather.gov/cap/us.php?x=1";

            var props = Props.Create<WeatherGovItemActor>();
            var actor = Context.ActorOf(props, "workers");

            int toProcess = 0;
            int processed = 0;

            Context.System.Scheduler.ScheduleTellRepeatedly(TimeSpan.FromSeconds(5),
                        TimeSpan.FromMinutes(5), Self,
                        new processFeedMessage(), Self);

            List<WeatherGovActiveItem> activeItems = new List<WeatherGovActiveItem>();

            Receive<processedWeatherGov>(e =>
            {
                if (e.item != null)
                {
                    // if null, then not an active item (cancelled or expired or past expiration time)
                    activeItems.Add(new WeatherGovActiveItem()
                    {
                        id = e.item.id,
                        title = e.item.title,
                        areaDesc = e.item.areaDesc,
                        updated = e.item.updated,
                        partionKey = e.item.partionKey
                    });
                }

                processed++;
                if (processed % 100 == 0)
                    Console.WriteLine(" processed " + processed);

                if (processed == toProcess)
                {                    
                    Program.cdb.UpsertDocument(new WeatherGovActive()
                    {
                        active = activeItems
                    }, "weathergov")
                    .Wait();
                    
                    Console.WriteLine("Finished processing WeatherGov");
                }
            });

            Receive<processFeedMessage>(m =>
            {
                string xml = "";
                activeItems.Clear();
                processed = 0;
                toProcess = 0;

                Console.WriteLine("Downloading data - WeatherGov");
                try
                {
                    WebClient wc = new WebClient();
                    wc.Headers.Add(HttpRequestHeader.Host, "alerts.weather.gov");
                    wc.Headers.Add(HttpRequestHeader.Pragma, "no-cache");
                    wc.Headers.Add(HttpRequestHeader.CacheControl, "no-cache");
                    wc.Headers.Add(HttpRequestHeader.UserAgent, "Win10");
                    wc.Headers.Add(HttpRequestHeader.Accept, "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,image/apng,*/*;q=0.8");

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
                        var el = xdoc.Root.Elements("{http://www.w3.org/2005/Atom}entry").ToList();

                        Console.WriteLine("Elements to process: " + el.Count());
                        toProcess += el.Count();
                        foreach (var e in el)
                        {
                            actor.Tell(new ProcessWeatherItem()
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

    internal class ProcessWeatherItem
    {
        public ProcessWeatherItem()
        {
        }

        public object item { get; set; }
    }

    internal class processFeedMessage
    {
    }

    internal class processedWeatherGov
    {
        internal WeatherGovItem item;
    }
}
