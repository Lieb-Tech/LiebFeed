using Akka.Actor;
using HtmlAgilityPack;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Xml.Linq;

namespace LiebFeed.InciWeb
{
    public class InciWebItemActor : ReceiveActor
    {        
        public InciWebItemActor()
        {
            Receive<processInciWebItem>(z =>
            {
                var item = new InciWebItem()
                {
                    link = z.Item.Elements("link").First().Value,
                    title = z.Item.Elements("title").First().Value,
                    description = z.Item.Elements("description").First().Value,
                    lat = z.Item.Elements("{http://www.w3.org/2003/01/geo/wgs84_pos#}lat").First().Value,
                    lng = z.Item.Elements("{http://www.w3.org/2003/01/geo/wgs84_pos#}long").First().Value,
                };

                var id = z.Item.Elements("guid").First().Value;
                id = id.Substring(0, id.Length - 1);
                item.id = id.Substring(id.LastIndexOf('/') + 1);

                item.point = new Microsoft.Azure.Documents.Spatial.Point(double.Parse(item.lng), double.Parse(item.lat));
                item.published = DateTimeOffset.Parse(z.Item.Elements("published").First().Value);
                item.pubDate = DateTimeOffset.Parse(z.Item.Elements("pubDate").First().Value);
                item.originalXML = z.Item.ToString();
                item.partionKey = item.id;

                WebClient wc = new WebClient();
                var html = wc.DownloadString(item.link);
                HtmlDocument doc = new HtmlDocument();
                doc.LoadHtml(html);

                IEnumerable<HtmlNode> nodes = doc.DocumentNode.Descendants(0).Where(n => n.Id == "IncidentInformation").ToList();
                var tds = nodes.First().SelectNodes("//td").ToList();
                var idx = tds.Select((n, i) => new { n, i }).Where(w => w.n.InnerHtml == "Incident Type").FirstOrDefault();
                if (idx != null)
                    item.incidentType = tds[idx.i + 1].InnerText;

                idx = tds.Select((n, i) => new { n, i }).Where(w => w.n.InnerHtml == "Planned Actions").FirstOrDefault();
                if (idx != null)
                    item.outlook = tds[idx.i + 1].InnerText;

                idx = tds.Select((n, i) => new { n, i }).Where(w => w.n.InnerHtml == "Size").FirstOrDefault();
                if (idx != null)
                    item.size = tds[idx.i + 1].InnerText;

                var curQry = Program.cdb.GetDocumentQuery<InciWebItem>("inciweb")
                    .Where(w => w.id == item.id
                            && w.partionKey == item.id);

                var cur = curQry.ToList();

                if (Program.forceSave)
                    cur.Clear();

                if (cur.Any())
                {
                    var dif = cur[0].description != item.description;
                    if (!dif && (cur[0].lat != item.lat || cur[0].lng != item.lng))
                        dif = true;

                    if (!dif && cur[0].title != item.title)
                        dif = false;

                    if (!dif && cur[0].size != item.size)
                        dif = false;

                    if (!dif && cur[0].outlook != item.outlook)
                        dif = false;

                    if (!dif && cur[0].incidentType != item.incidentType)
                        dif = false;

                    if (dif)
                    {
                        Program.cdb.UpsertDocument(item, "inciweb").Wait();
                        Console.WriteLine("   updated " + item.id);

                        Program.cdb.UpsertDocument(new CommonDataFormat()
                        {
                            id = Guid.NewGuid().ToString(),
                            partionKey = "inciweb",
                            source = "inciweb",
                            title = item.title,
                            extra = item.description,
                            point = null,
                            pubDate = item.pubDate,
                            sourceId = item.id,
                            sourcePk = item.partionKey
                        }, "commondata").Wait();
                    }
                }
                else
                {
                    Program.cdb.UpsertDocument(item, "inciweb").Wait();
                    Console.WriteLine("   saved " + item.id);

                    Program.cdb.UpsertDocument(new CommonDataFormat()
                    {
                        id = Guid.NewGuid().ToString(),
                        partionKey = "inciweb",
                        source = "inciweb",
                        title = item.title,
                        extra = item.description,
                        point = null,
                        pubDate = item.pubDate,
                        sourceId = item.id,
                        sourcePk = item.partionKey
                    }, "commondata").Wait();
                }               

                Sender.Tell(new processedInciWeb() { item = item });                
            });
        }
    }

    internal class processInciWebItem
    {
        public XElement Item;
        public processInciWebItem(XElement item)
        {
            Item = item;
        }
    }
}
