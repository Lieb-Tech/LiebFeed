using Akka.Actor;
using System;
using System.Collections.Generic;
using System.Text;

namespace LiebFeed.NCMC
{
    public class NCMCItemActor : ReceiveActor
    {
        public NCMCItemActor()
        {
            Receive<ProcessNCMCItem>(r =>
            {
                var ncmc = new NCMCItem();
                ncmc.title = r.item.Element("title").Value;
                var pubDate = r.item.Element("pubDate").Value;
                ncmc.link = r.item.Element("link").Value;
                var description = r.item.Element("description").Value;
                ncmc.description = description;
                ncmc.img = "https://api.missingkids.org/" + r.item.Element("enclosure").Attribute("url").Value;

                var id = ncmc.link.Substring(ncmc.link.IndexOf("MC/") + 3); //  1351732/1
                ncmc.id = id.Substring(0, id.Length - 2);
                ncmc.partionKey = ncmc.id;

                var idx = description.IndexOf(",");
                ncmc.name = description.Substring(0, idx).Trim();
                idx = description.IndexOf(":");
                var idx2 = description.IndexOf(",", idx);
                ncmc.age = description.Substring(idx + 1, idx2 - idx - 1).Trim();

                idx = description.IndexOf(":", idx2);
                idx2 = description.IndexOf(".", idx);
                ncmc.missing = description.Substring(idx + 1, idx2 - idx - 1).Trim();

                idx = description.IndexOf("From", idx2) + 4;
                idx2 = description.IndexOf(".", idx);
                ncmc.from = description.Substring(idx + 1, idx2 - idx - 1).Trim();

                idx = description.IndexOf("CONTACT:", idx2) + 8;
                idx2 = description.IndexOf("(", idx);
                ncmc.contact = description.Substring(idx + 1, idx2 - idx - 1).Trim();

                idx = description.IndexOf(")", idx);
                idx2 = description.IndexOf(".", idx);
                ncmc.phone = description.Substring(idx + 1, idx2 - idx - 1).Trim();

                ncmc.state = ncmc.title.Substring(ncmc.title.Length - 3, 2).Trim();
                ncmc.originalXML = r.ToString();

                try
                {
                    Program.cdb.UpsertDocument(ncmc, "ncmc").Wait();

                    Program.cdb.UpsertDocument(new CommonDataFormat()
                    {
                        id = Guid.NewGuid().ToString(),
                        partionKey = "ncmc",
                        source = "ncmc",
                        title = ncmc.title,
                        extra = ncmc.description,
                        point = null,
                        pubDate = DateTimeOffset.Parse(ncmc.pubDate),
                        sourceId = ncmc.id,
                        sourcePk = ncmc.partionKey
                    }, "commondata").Wait();
                }
                catch (Exception ex)
                {
                    System.Threading.Thread.Sleep(5000);
                    Program.cdb.UpsertDocument(ncmc, "ncmc").Wait();
                }                

                Sender.Tell(new itemProcessed() { item = ncmc });
            });            
        }
    }
}
