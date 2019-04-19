using Akka.Actor;
using HtmlAgilityPack;
using System;
using System.Linq;
using System.Net;

namespace LiebFeed.CSPC
{
    internal class CPSCItemActor : ReceiveActor
    {
        public CPSCItemActor()
        {
            Receive<ProcessCPSCItem>(z =>
            {
                var item = new CPSCItem()
                {
                    link = z.item.Element("link").Value,
                    title = z.item.Element("title").Value,
                    description = z.item.Element("description").Value,
                    pubDate = z.item.Element("pubDate").Value,
                };

                item.partionKey = item.id;

                WebClient wc = new WebClient();
                var html = wc.DownloadString(item.link);
                HtmlDocument doc = new HtmlDocument();
                doc.LoadHtml(html);

                var main = doc.DocumentNode.SelectNodes("//*[@role=\"listbox\"]");
                var img = main.First().ChildNodes.First().ChildNodes.First().ChildNodes;
                item.imgSrc = img.First().Attributes.Where(w => w.Name == "src").ToList().First().Value;
                
                main = doc.DocumentNode.SelectNodes("//*[@class=\"summary_section_content\"]");
                var ele = main.First().ChildNodes.Where(w => w.NodeType == HtmlNodeType.Element).ToList();
                if (ele.Any(a => a.InnerText.Contains("Hazard")))
                {
                    var haz = ele.First(f => f.InnerText.Contains("Hazard")).InnerText.Replace('\r', ' ');
                    var val = haz.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                    item.hazard = val[1].Trim();
                }

                var main2 = doc.DocumentNode.SelectNodes("//*[@class=\"details_section_content\"]");
                ele = main2.First().ChildNodes.Where(w => w.NodeType == HtmlNodeType.Element).ToList();
                var des = ele.First(f => f.InnerText.Contains("Description")).InnerText.Replace('\r',' ');
                var vals = des.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                item.fullDescription = vals[1].Trim();

                if (ele.Any(f => f.InnerText.Contains("Remedy")))
                {
                    des = ele.First(f => f.InnerText.Contains("Remedy")).InnerText.Replace('\r', ' ');
                    vals = des.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                    item.remedy = vals[1].Trim();
                }

                if (ele.Any(f => f.InnerText.Contains("Manufacturer(s)")))
                {
                    des = ele.First(f => f.InnerText.Contains("Manufacturer(s)")).InnerText.Replace('\r', ' ');
                    vals = des.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                    item.manufacture = vals[1].Trim();
                }

                if (ele.Any(f => f.InnerText.Contains("Sold At")))
                {
                    des = ele.First(f => f.InnerText.Contains("Sold At")).InnerText.Replace('\r', ' ');
                    vals = des.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                    item.soldAt = vals[1].Trim();
                }

                if (ele.Any(f => f.InnerText.Contains("Recall number")))
                {
                    des = ele.First(f => f.InnerText.Contains("Recall number")).InnerText.Replace('\r', ' ');
                    vals = des.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                    item.id = vals[1].Trim();
                    item.partionKey = item.id;
                }

                item.originalXML = z.ToString();

                // TODO: Only save if not in the DB already
                Program.cdb.UpsertDocument(item, "cpsc").Wait();

                Sender.Tell(new processedCSPC() { item = item });

                Program.cdb.UpsertDocument(new CommonDataFormat()
                {
                    id = Guid.NewGuid().ToString(),
                    partionKey = "cpsc",
                    source = "cpsc",
                    title = item.title,
                    extra = item.description,
                    point = null,
                    pubDate = DateTimeOffset.Parse(item.pubDate),
                    sourceId = item.id,
                    sourcePk = item.partionKey
                }, "commondata").Wait();
            });            
        }
    }
}