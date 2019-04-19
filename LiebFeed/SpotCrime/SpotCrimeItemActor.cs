using Akka.Actor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LiebFeed.SpotCrime
{
    public class SpotCrimeItemActor : ReceiveActor
    {
        public SpotCrimeItemActor()
        {
            Receive<processSCItem>(r =>
            {
                var crimeData = new SpotCrimeItem()
                {
                    link = r.item.Element("link").Value,
                    title = r.item.Element("title").Value,
                    description = r.item.Element("description").Value,
                    pubDate = DateTimeOffset.Parse(r.item.Element("pubDate").Value),
                };

                var g = crimeData.link.Substring(crimeData.link.LastIndexOf("/?") + 2);
                crimeData.id = g;
                crimeData.partionKey = r.location;

                if (r.item.Elements().Any(z => z.Name.LocalName == "point"))
                {
                    var point = r.item.Elements().First(z => z.Name.LocalName == "point").Value;
                    crimeData.point = new Microsoft.Azure.Documents.Spatial.Point(
                        float.Parse(point.Substring(point.IndexOf(' ') + 1)),
                        float.Parse(point.Substring(0, point.IndexOf(' '))));
                }
               
                var qry = Program.cdb.GetDocumentQuery("spotcrime", "select c.id from c where c.id = '" + crimeData.id + "' and c.partionKey = '" + crimeData.partionKey + "'");
           
                var vals = qry.ToList();

                if (!vals.Any())
                {
                    try
                    {
                        Program.cdb.UpsertDocument(crimeData, "spotcrime").Wait();
                    }
                    catch (Exception ex)
                    {
                        System.Threading.Thread.Sleep(5000);
                        Program.cdb.UpsertDocument(crimeData, "spotcrime").Wait();
                    }
                }

                Sender.Tell(new processedCSItem() { location = crimeData.partionKey, newItem = vals.Any() });
            });
        }
    }
}
