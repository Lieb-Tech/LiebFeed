using Akka.Actor;
using LiebFeed.Police.Seattle;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LiebFeed.Police
{
    class SeattlePDItemActor : ReceiveActor
    {
        public SeattlePDItemActor()
        {
            Receive<processSPDItem>(r =>
            {
                var item = XMLSerializeHelper.Deserialize<Item>(r.item.ToString());

                var c = new SeattlePDItem()
                {
                    category = item.Category,
                    creator = item.Creator.Text,
                    description = item.Encoded.Text
                        .Replace("<p>", "")
                        .Replace("</p>", "\r\n")
                        .Replace("&#8217;", "'")
                        .Replace("&#8243;", "\"")
                        .Replace("&#8230;", "…"),
                    link = item.Link,
                    pubDate = DateTimeOffset.Parse(item.PubDate),
                    title = item.Title,
                    id = "seattlewa:" + item.Guid.Text.Substring(item.Guid.Text.IndexOf("=") + 1),
                    originalXML = r.item.ToString() 
                };
                c.partionKey = "seattlewa";

                var idx = c.description.IndexOf("For additional tips ");                
                if (idx > 0)
                    c.description = c.description.Substring(0, idx - 1);

                var startsHTML = c.description.StartsWith("<");
                if (startsHTML)
                {
                    c.description = c.description.Substring(c.description.IndexOf("\r\n") + 1).Trim();
                }

                var str = c.description.IndexOf("<", 10);
                if (str > 0)
                    c.description = c.description.Substring(0, str - 1);

                var qry = Program.cdb.GetDocumentQuery<SeattlePDItem>("police")
                    .Where(s => s.id == c.id && c.partionKey == c.id)
                    .Select(s => s.id);

                var results = qry.ToList();
                results.Clear();
                if (!results.Any())
                {
                    Program.cdb.UpsertDocument(c, "police").Wait();

                    Program.cdb.UpsertDocument(new
                    {
                        id = "recent",
                        partionKey = "seattlewa",                        
                    }, "police").Wait();
                }  

                Sender.Tell(new itemProcessed() { item = c });
            });
        }
    }
}
