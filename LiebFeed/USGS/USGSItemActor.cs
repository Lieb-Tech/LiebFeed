using Akka.Actor;
using HtmlAgilityPack;
using LiebFeed.USGS.FeedDataStructures;
using Microsoft.Azure.Documents.Client;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace LiebFeed.USGS
{
    public class ProcessUSGSItem
    {
        public string path;
        public XElement item;
    }

    public class USGSItemActor : ReceiveActor
    {        
        public USGSItemActor()
        {
            Receive<ProcessUSGSItem>(e =>
            {
                var item = XMLSerializeHelper.Deserialize<FeedEntry>(e.item.ToString());
                var usgs = new FeedDataStructures.USGSItem();
                var html = new HtmlDocument();
                html.LoadHtml(item.Summary.Text);

                var orig = html.DocumentNode.SelectNodes("//dd").Select(z => z.InnerText).First();
                var dt = DateTimeOffset.Parse(orig.Substring(0, orig.Length - 3));
                usgs.id = item.Id.Substring(item.Id.LastIndexOf(":") + 1);
                usgs.partionKey = usgs.id + ":" + dt.ToString("yyyy-MM-dd");
                usgs.published = dt;
                usgs.updated = DateTimeOffset.Parse(item.Updated);

                usgs.magnitude = processMag(item.Title);
                usgs.point = processPoint(item.Point);
                usgs.elevation = float.Parse(item.Elev) / 1000f;
                usgs.link = item.Link.Href;
                usgs.originalXML = e.ToString();
                usgs.title = item.Title;
                usgs.source = e.path.Substring(0, e.path.IndexOf('.'));

                if (updateIfNew(usgs))
                {
                    Program.cdb.UpsertDocument(new CommonDataFormat()
                    {
                        id = Guid.NewGuid().ToString(),
                        partionKey = "usgs",
                        source = "usgs",
                        title = usgs.title,
                        extra = usgs.summary,
                        point = usgs.point,
                        pubDate = usgs.published,
                        sourceId = usgs.id,
                        sourcePk = usgs.partionKey
                    }, "commondata").Wait();

                    if ((DateTimeOffset.UtcNow - usgs.updated).TotalSeconds > 10)
                    {
                        Sender.Tell(new processRecent(usgs));
                    }
                }

                Sender.Tell(new itemProcessed());                
            });
        }        

        private float processMag(string title)
        {
            float m = 0;
            var val = title.Substring(1).Trim();
            float.TryParse(val.Substring(0, val.IndexOf(" ")), out m);
            return m;
        }

        private Microsoft.Azure.Documents.Spatial.Point processPoint(string point)
        {
            return new Microsoft.Azure.Documents.Spatial.Point(
                float.Parse(point.Substring(point.IndexOf(' ') + 1)),
                float.Parse(point.Substring(0, point.IndexOf(' ')))
            );
        }

        private bool updateIfNew(FeedDataStructures.USGSItem usgs)
        {
            FeedOptions queryOptions = new FeedOptions { MaxItemCount = -1, EnableCrossPartitionQuery = true };

            // get updated to see if (1) in the DB and (2) if so, if this is newer than db version
            var qryUpd = Program.cdb.GetDocumentQuery<USGSItem>("usgs")
                            .Where(z => z.id == usgs.id && z.partionKey == usgs.partionKey)
                            .Select(z => z.updated);

            var vals = qryUpd.ToList();

            if (Program.forceSave)
                vals.Clear();


            // not there, create both main and archive
            if (!vals.Any())
            {
                Console.WriteLine("   adding " + usgs.id);
                Program.cdb.UpsertDocument(usgs, "usgs").Wait();

                var arch = new USGSItemArchive()
                {
                    id = "archive:" + usgs.id,
                    partionKey = usgs.partionKey,
                    updates = new List<FeedDataStructures.USGSItem>() { usgs }
                };
                Program.cdb.UpsertDocument(arch, "usgs").Wait();                            
                return true;
            }
            else if (vals.First() != usgs.updated)
            {
                Console.WriteLine("   updating " + usgs.id);

                // is in the DB, and the updated is different than the value in the DB
                var archiveQuery = Program.cdb.GetDocumentQuery<USGSItemArchive>("usgs")
                    .Where(z => z.id == "archive:" + usgs.id && z.partionKey == usgs.partionKey);

                var archives = archiveQuery.ToList();
                if (archives.Any())
                {
                    if (!archives.First().updates.Any(z => z.updated == usgs.updated))
                        archives.First().updates.Add(usgs);

                    Program.cdb.UpsertDocument(usgs, "usgs").Wait();
                    Program.cdb.UpsertDocument(archives.First(), "usgs").Wait();

                    // submit to EventHub for further processing
                    return true;
                }
            }
            return false;
        }        
    }
}