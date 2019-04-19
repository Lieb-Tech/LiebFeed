using Akka.Actor;
using LiebFeed.Helpers;
using LiebFeed.NLPHelper;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LiebFeed.Reuters
{
    public class ReutersItemActor : ReceiveActor
    {
        List<string> idsProcessed = new List<string>();

        protected override void PreStart()
        {
            var dt = DateTimeOffset.UtcNow.AddDays(-1).ToString("yyyy-MM-ddThh:mm:ss+00");
            var ids = Program.cdb.GetDocumentQuery("newsfeed", "select value c.id from c where c.partionKey ='Reuters' and c.pubDate > '" + dt + "'");
            var res = ids.ToList();
            idsProcessed = res.Select(a => (a as string)).ToList();

            base.PreStart();
        }

        public ReutersItemActor()
        {
            List<ReutersItem> items = new List<ReutersItem>();

            // Step 1
            Receive<processReutersItem>(i =>
            {
                var id = Helpers.GeneralHelper.IdHelper(i.item.Element("guid").Value);
                if (!idsProcessed.Contains(id))
                {
                    idsProcessed.Add(id);
                    var d = i.item.Element("description").Value;
                    d = d.DeEscape();

                    if (d.IndexOf("<") > 0)
                    {
                        var item = new ReutersItem()
                        {
                            id = id,
                            partionKey = "Reuters",
                            link = i.item.Element("link").Value,
                            pubDate = DateTimeOffset.Parse(i.item.Element("pubDate").Value),
                            title = i.item.Element("title").Value.DeEscape(),
                            description = d.Substring(0, d.IndexOf("<") - 1),
                            origXML = i.item.ToString(),
                            siteSection = i.item.Element("category").Value
                        };

                        items.Add(item);
                        Program.stopActor.Tell(new NLPHelper.StopwordRequest()
                        {
                            id = item.id,
                            linesOfText = new List<string>() { item.title, item.description }
                        });
                    }
                    else
                        Context.Parent.Tell(new processedReuters());
                }
                else
                    Context.Parent.Tell(new processedReuters());
            });

            // Step 2
            Receive<NLPHelper.StopwordResponse>(r =>
            {
                var item = items.First(z => z.id == r.id);
                item.swTitle = r.outputStrings[0];
                item.swDescription = r.outputStrings[1];

                var n = new SharedMessages.SentimentRequest()
                {
                    id = r.id,
                    feed = "Reuters",
                    linesToProcess = new List<string>()
                    {
                        item.title,
                        item.description
                    }
                };

                var remote2 = Context.ActorSelection("akka.tcp://nlp-system@localhost:8080/user/sent");
                var req2 = JsonConvert.SerializeObject(n);
                remote2.Tell(req2);
            });
            
            // step 3 || step 4
            Receive<string>(r =>
            {
                if (r.StartsWith("sent:"))
                {
                    var sent = JsonConvert.DeserializeObject<SharedMessages.SentimentResponse>(r.Substring(5));

                    var item = items.First(z => z.id == sent.id);
                    item.sentTitle = sent.results[0];
                    item.sentiDescription = sent.results[1];

                    var req = new StemmingRequest()
                    {
                        id = item.id,
                        linesOfText = new List<string>()
                    {
                        item.swTitle,
                        item.swDescription,
                    }
                    };
                    Program.stemActor.Tell(req);
                }
                else if (r.StartsWith("ner:"))
                {
                    var sent = JsonConvert.DeserializeObject<SharedMessages.NERResponse>(r.Substring(4));
                    var item = items.First(z => z.id == sent.id);

                    item.nerTitle = sent.results[0];
                    item.nerDescription = sent.results[1];

                    Program.nerCountActor.Tell(sent);

                    Program.stemActor.Tell(new NLPHelper.StemmingRequest()
                    {
                        id = item.id,
                        linesOfText = new List<string>() { item.title, item.description }
                    });
                }
            });

            // step 5
            Receive<NLPHelper.StemmingResponse>(r =>
            {
                var item = items.First(z => z.id == r.id);
                item.stemmedTitle = r.lines[0];
                item.stemmedDescription = r.lines[1];

                Self.Tell(new processedReuters()
                {
                    id = r.id
                });
            });

            Receive<processedReuters>(r =>
            {
                var item = items.First(z => z.id == r.id);
                items.Remove(item);

                Program.cdb.UpsertDocument(item, "newsfeed").Wait();                

                Console.WriteLine("=========> Reuters saving " + item.siteSection);

                Program.wordCountActor.Tell(new NLPHelper.CountRequest()
                {
                    Feed = "Reuters",
                    Id = item.id,
                    LineOFtext = item.stemmedDescription
                });

                Program.nerActor.Tell(new SharedMessages.NERRequest()
                {
                    id = item.id,
                    feed = "Reuters",
                    linesToProcess = new List<string>()
                    {
                        item.title,
                        item.description
                    }
                });

                Context.Parent.Tell(new processedReuters());

            });
        }
    }
}
