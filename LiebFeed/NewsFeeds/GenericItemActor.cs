using Akka.Actor;
using LiebFeed.Helpers;
using LiebFeed.NLPHelper;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LiebFeed.NewsFeeds
{
    class GenericItemActor : ReceiveActor
    {
        List<string> idsProcessed = new List<string>();

        protected override void PreStart()
        {
            var dtStart = DateTimeOffset.UtcNow.AddDays(-1).ToString("yyyy-MM-ddThh:mm:ss+00");
            // 2019-03-27T22:54:10+00:00        
            var ids = Program.cdb.GetDocumentQuery("newsfeed", "select value c.id from c where c.pubDate > '" + dtStart + "'");
            var res = ids.ToList();
            idsProcessed = res.Select(a => (a as string)).ToList();

            base.PreStart();
        }

        public GenericItemActor()
        {
            List<newstem> items = new List<newstem>();

            Receive<processItem>(i =>
            {
                if (i.item.Element("guid") != null)
                {
                    var id = Helpers.GeneralHelper.IdHelper(i.item.Element("guid").Value);
                    id = Helpers.GeneralHelper.IdHelper(id);
                    if (!idsProcessed.Contains(id))
                    {
                        idsProcessed.Add(id);
                        var d = i.item.Element("description").Value;
                        d = d.DeEscape();

                        if ((d.IndexOf("<") > 0 || d.IndexOf("<") == -1) && !d.StartsWith("http"))
                        {
                            DateTimeOffset dt = DateTimeOffset.Now;
                            // "Sat, 30 Mar 2019 14:26:25 -0400"
                            if (i.item.Element("pubDate") != null)
                            {
                                var pub = i.item.Element("pubDate").Value;
                                // pub = pub.Substring(0, pub.Length - 5).Trim() + " +0000";
                                dt = DateTimeOffset.Parse(pub);
                            }

                            var item = new newstem()
                            {
                                id = id,
                                partionKey = i.feed,
                                link = i.item.Element("link").Value,
                                pubDate = dt,
                                title = i.item.Element("title").Value.DeEscape(),
                                description = (d.IndexOf("<") > -1 ? d.Substring(0, d.IndexOf("<") - 1) : d),
                                origXML = i.item.ToString(),
                                siteSection = i.section
                            };

                            items.Add(item);
                            Program.stopActor.Tell(new NLPHelper.StopwordRequest()
                            {
                                id = item.id,
                                linesOfText = new List<string>() { item.title, item.description }
                            });
                        }
                        else
                            Context.Parent.Tell(new processedItem());
                    }
                    else
                        Context.Parent.Tell(new processedItem());
                }
                else
                    Context.Parent.Tell(new processedItem());
            });

            //Step 2
            Receive<NLPHelper.StopwordResponse>(r =>
            {
                var item = items.First(z => z.id == r.id);
                item.swTitle = r.outputStrings[0];
                item.swDescription = r.outputStrings[1];

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

            });

            // Step 3
            Receive<NLPHelper.StemmingResponse>(r =>
            {
                var item = items.First(z => z.id == r.id);
                item.stemmedTitle = r.lines[0];
                item.stemmedDescription = r.lines[1];

                var n = new SharedMessages.SentimentRequest()
                {
                    id = r.id,
                    feed = item.partionKey,
                    linesToProcess = new List<string>()
                    {
                        item.title,
                        item.description
                    }
                };
                var remote = Context.ActorSelection("akka.tcp://nlp-system@localhost:8080/user/sent");
                var req = JsonConvert.SerializeObject(n);
                remote.Tell(req);
            });

            // Step 4 = sentiment || Step 5 = ner 
            Receive<string>(r =>
            {
                if (r.StartsWith("sent:"))
                {
                    var sent = JsonConvert.DeserializeObject<SharedMessages.SentimentResponse>(r.Substring(5));
                    var item = items.First(z => z.id == sent.id);
                    if (sent.results.Count > 1)
                    {
                        item.sentTitle = sent.results[0];
                        item.sentDescription = sent.results[1];
                    }
                    else if (string.IsNullOrWhiteSpace(item.description))
                        item.sentTitle = sent.results[0];
                    else
                        item.sentDescription = sent.results[0];

                    var n = new SharedMessages.NERRequest()
                    {
                        id = sent.id,
                        feed = item.partionKey,
                        linesToProcess = new List<string>()
                        {
                            item.title,
                            item.description
                        }
                    };
                    var remote = Context.ActorSelection("akka.tcp://nlp-system@localhost:8080/user/ner");
                    var req = JsonConvert.SerializeObject(n);
                    remote.Tell(req);
                }
                else if (r.StartsWith("ner:"))
                {
                    var sent = JsonConvert.DeserializeObject<SharedMessages.NERResponse>(r.Substring(4));
                    var item = items.First(z => z.id == sent.id);

                    item.nerTitle = sent.results[0];
                    item.nerDescription = sent.results[1];

                    Program.nerCountActor.Tell(sent);

                    Self.Tell(new processedItem() { id = sent.id });
                }
            });

            // done!
            Receive<processedItem>(r =>
            {
                var item = items.First(z => z.id == r.id);

                Program.cdb.UpsertDocument(item, "newsfeed").Wait();
                items.Remove(item);

                Console.WriteLine("=========> " + item.partionKey + " saving " + item.siteSection);

                Program.wordCountActor.Tell(new NLPHelper.CountRequest()
                {                     
                    Feed = item.partionKey,
                    Id = item.id,
                    LineOFtext = item.stemmedDescription
                });

                Program.nerActor.Tell(new SharedMessages.NERRequest()
                {
                    id = item.id,
                    feed = item.partionKey,
                    linesToProcess = new List<string>()
                    {
                        item.title,
                        item.description
                    }
                });

                Context.Parent.Tell(new processedItem());
            });
        }
    }
}
