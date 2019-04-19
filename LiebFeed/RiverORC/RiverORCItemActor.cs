using Akka.Actor;
using HtmlAgilityPack;
using Microsoft.Azure.Documents.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LiebFeed.RiverORC
{
    class RiverORCItemActor : ReceiveActor
    {
        IActorRef lookupActor;
        List<RiverORCItem> itemsWaiting = new List<RiverORCItem>();

        public RiverORCItemActor()
        {
            lookupActor = Context.ActorOf<RiverORCGaugeActor>("riverGuageInfo");

            Receive<RiverORC.ProcessORCItem>(p =>
            {                
                var item = new RiverORCItem()
                {
                    description = p.item.Element("description").Value,
                    link = p.item.Element("link").Value,
                    originalXML = p.item.ToString(),
                    pubDate = DateTimeOffset.Parse(p.item.Element("pubDate").Value),
                    title = p.item.Element("title").Value,                    
                };
                item.gauge = item.link.Substring(item.link.LastIndexOf("=") + 1);                
                item.id = p.state + ":" + item.link.Substring(item.link.LastIndexOf("=") + 1);
                item.partionKey = p.state;
                
                var html = new HtmlDocument();
                html.LoadHtml(item.description);

                if (!item.description.Contains("<div>No flood categories have been defined for this gauge.</div>"))
                {
                    var vals = html.DocumentNode.SelectSingleNode("//h4");
                    if (vals != null)
                    {
                        var lis = vals.SelectNodes("//li").Take(4).ToArray();

                        if (lis[0].InnerHtml != "Action  : Not Set")
                        {
                            var action = lis[0].InnerHtml.Substring(lis[0].InnerHtml.IndexOf(":") + 1).Trim();
                            var minor = lis[1].InnerHtml.Substring(lis[1].InnerHtml.IndexOf(":") + 1).Trim();
                            var moderate = lis[2].InnerHtml.Substring(lis[2].InnerHtml.IndexOf(":") + 1).Trim();
                            var major = lis[3].InnerHtml.Substring(lis[3].InnerHtml.IndexOf(":") + 1).Trim();

                            if (action.EndsWith("ft")) action = action.Substring(0, action.Length - 3);
                            if (action.EndsWith("kcfs")) action = action.Substring(0, action.Length - 5);

                            if (minor.EndsWith("ft")) minor = minor.Substring(0, minor.Length - 3);
                            if (minor.EndsWith("kcfs")) minor = minor.Substring(0, minor.Length - 5);

                            if (moderate.EndsWith("ft")) moderate = moderate.Substring(0, moderate.Length - 3);
                            if (moderate.EndsWith("kcfs")) moderate = moderate.Substring(0, moderate.Length - 5);

                            if (major.EndsWith("ft")) major = major.Substring(0, major.Length - 3);
                            if (major.EndsWith("kcfs")) major = major.Substring(0, major.Length - 5);

                            if (action != "Not Set") item.action = double.Parse(action);
                            if (minor != "Not Set") item.minor = double.Parse(minor);
                            if (moderate != "Not Set") item.moderate = double.Parse(moderate);
                            if (major != "Not Set") item.major = double.Parse(major);
                        }

                        
                    }
                    var readings = html.DocumentNode.SelectNodes("//div");
                    var latest = readings.Where(z => z.InnerHtml.StartsWith("Latest Observation:")).FirstOrDefault();                    

                    if (latest != null && !latest.InnerHtml.Contains("N/A"))
                    {
                        var reading = latest.InnerHtml.Substring(20);
                        if (reading.EndsWith("ft")) reading = reading.Substring(0, reading.Length - 3);
                        if (reading.EndsWith("kcfs")) reading = reading.Substring(0, reading.Length - 5);
                        item.latest = double.Parse(reading);
                    }

                    if (readings.Any(z => z.InnerHtml.StartsWith("Latest Observation Category: ")))
                    {
                        var stage = readings.FirstOrDefault(z => z.InnerHtml.StartsWith("Latest Observation Category: "));
                        if (stage != null)
                        {
                            item.stage = stage.InnerHtml.Substring(stage.InnerHtml.IndexOf(":") + 1).Trim();
                        }
                    }

                    if (item.action + item.minor + item.moderate + item.major == 0)
                        Sender.Tell(new processedORC());
                    else
                    {
                        itemsWaiting.Add(item);
                        lookupActor.Tell(new RiverORCLookup() { gauge = item.gauge, state = p.state });
                    }
                }
                else 
                    Sender.Tell(new processedORC());
                var a = "";
            });

            Receive<RiverGaugeInfo>(z =>
            {
                bool save = false;
                var item = itemsWaiting.Where(i => z.gauge == i.gauge).First();
                item.point = z.point;

                var qry = Program.cdb.GetDocumentQuery<RiverORCItem>("riverorc")
                .Where(q => q.id == item.id && q.partionKey == item.partionKey)
                .Select(q => new
                {
                    q.latest,
                    q.pubDate,
                    q.id
                });

                var res = qry.ToList();

                if (res.Any())
                {
                    var cur = res.First();
                    item.deltaLatest = Math.Round(item.latest - cur.latest, 3);
                    save = Math.Abs((item.pubDate - cur.pubDate).TotalMinutes) > 1;

                    if (save)
                    {
                        // current
                        Program.cdb.UpsertDocument(item, "riverorc").Wait();
                    }
                    
                }
                else
                {
                    item.deltaLatest = 0;
                    // current
                    Program.cdb.UpsertDocument(item, "riverorc").Wait();
                }

                if (save)
                {
                    // archive
                    item.id = "archive:" + item.id + ":" + item.pubDate.ToUnixTimeSeconds();
                    Program.cdb.UpsertDocument(item, "riverorc").Wait();
                }

                Context.Parent.Tell(new processedORC());

                itemsWaiting.Remove(item);
            });
        }
    }
}
