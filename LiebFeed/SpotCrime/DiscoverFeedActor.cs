using Akka.Actor;
using Akka.Routing;
using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LiebFeed.SpotCrime
{
    public class DiscoverFeedActor : ReceiveActor
    {
        public DiscoverFeedActor()
        {
            var url = "https://spotcrime.com/rss.php";
            Context.System.Scheduler.ScheduleTellRepeatedly(TimeSpan.FromSeconds(15),
                    TimeSpan.FromHours(6), Self,
                    new processDiscover(), Self);        

            Receive<processDiscover>(r =>
            {                
                // var props = Props.Create<SpotCrimeFeedActor>().WithRouter(new RoundRobinPool(2));                
                Dictionary<string, IActorRef> actors = new Dictionary<string, IActorRef>();

                
                var html = "";
                Console.WriteLine("Downloading data - SpotCrime");
                try
                {
                    WebClient wc = new WebClient();
                    html = wc.DownloadString(url);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Couldn't download data -- SpotCrime!!");
                }

                if (!string.IsNullOrWhiteSpace(html))
                {
                    try
                    {
                        HtmlDocument xdoc = new HtmlDocument();
                        xdoc.LoadHtml(html);
                        var tds = xdoc.DocumentNode.SelectNodes("//td").ToList().Where(z => z.InnerHtml.StartsWith("<a")).ToList();
                        
                        foreach(var t in tds)
                        {
                            var lnk = t.ChildNodes.First().Attributes.Where(a => a.Name == "href").First();

                            if (!actors.ContainsKey(lnk.Value))
                            {
                                actors.Add(lnk.Value, Context.ActorOf<SpotCrimeFeedActor>());
                            }
                            actors[lnk.Value].Tell(new processSCFeed()
                            {
                                url = lnk.Value
                            });

                            Thread.Sleep(5000);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Error parsin the data -- SpotCrime");
                    }
                }

            });
        }
    }

    internal class processDiscover
    {
    }
}
