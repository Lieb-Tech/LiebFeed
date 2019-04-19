using Akka.Actor;
using LiebFeed.Helpers;
using LiebFeed.NLPHelper;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LiebFeed.USAToday
{
    class USATodayItemActor : ReceiveActor
    {
        List<string> idsProcessed = new List<string>();

        protected override void PreStart()
        {
            var dt = DateTimeOffset.UtcNow.AddDays(-1).ToString("yyyy-MM-ddThh:mm:ss+00");
            var ids = Program.cdb.GetDocumentQuery("newsfeed", "select value c.id from c where c.partionKey = 'USAToday' and c.pubDate > '" + dt + "'");
            var res = ids.ToList();
            idsProcessed = res.Select(a => (a as string)).ToList();

            base.PreStart();
        }

        public USATodayItemActor()
        {
            List<USATodayItem> items = new List<USATodayItem>();            
            Receive<USAToday.processUSATodayItem>(i =>
            {                
                var id = Helpers.GeneralHelper.IdHelper(i.item.Element("guid").Value);
                id = Helpers.GeneralHelper.IdHelper(id);
                if (!idsProcessed.Contains(id))
                {
                    idsProcessed.Add(id);
                    var d = i.item.Element("description").Value;
                    d = d.DeEscape();

                    if (d.IndexOf("<") != 0)
                    {
                        var item = new USATodayItem()
                        {
                            id = i.item.Element("guid").Value.Replace("/", ""),
                            partionKey = "USAToday",
                            link = i.item.Element("link").Value,
                            pubDate = DateTimeOffset.Parse(i.item.Element("pubDate").Value),
                            title = i.item.Element("title").Value.DeEscape(),
                            description = d.IndexOf("<") == -1 ? d : d.Substring(0, d.IndexOf("<") - 1),
                            origXML = i.item.ToString(),
                        };

                        var lnk = i.item.Element("link").Value;
                        var idx1 = lnk.LastIndexOf("~");
                        if (idx1 != -1)
                        {
                            var idx2 = lnk.LastIndexOf("-", idx1);
                            // usatodaycomnation-topstories~                       
                            item.siteSection = item.link.Substring(idx2 + 1, idx1 - idx2 - 1);
                        }
                        else
                        {
                            item.siteSection = "unknown";
                        }

                        items.Add(item);
                        Program.stopActor.Tell(new NLPHelper.StopwordRequest()
                        {
                            id = item.id,
                            linesOfText = new List<string>() { item.title, item.description }
                        });
                    }
                    else
                        Context.Parent.Tell(new processedUSAToday());
                }
                else
                    Context.Parent.Tell(new processedUSAToday());
            });

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

            Receive<NLPHelper.StemmingResponse>(r =>
            {
                var item = items.First(z => z.id == r.id);
                item.stemmedTitle = r.lines[0];
                item.stemmedDescription = r.lines[1];

                var n = new SharedMessages.SentimentRequest()
                {
                    id = r.id,
                    feed = "USAToday",
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

                    if (string.IsNullOrWhiteSpace(item.title))
                    {
                        item.sentiDescription = sent.results[0];
                    }
                    else if (string.IsNullOrWhiteSpace(item.description))
                    {
                        item.sentTitle = sent.results[0];
                    }
                    else
                    {
                        item.sentTitle = sent.results[0];
                        item.sentiDescription = sent.results[1];
                    }

                    var n = new SharedMessages.NERRequest()
                    {
                        id = item.id,
                        feed = "USAToday",
                        linesToProcess = new List<string>()
                        {
                            item.title,
                            item.description
                        }
                    };

                    var remote2 = Context.ActorSelection("akka.tcp://nlp-system@localhost:8080/user/ner");
                    var req2 = JsonConvert.SerializeObject(n);
                    remote2.Tell(req2);
                }
                else if (r.StartsWith("ner:"))
                {
                    var sent = JsonConvert.DeserializeObject<SharedMessages.NERResponse>(r.Substring(4));
                    var item = items.First(z => z.id == sent.id);

                    item.nerTitle = sent.results[0];
                    item.nerDescription = sent.results[1];

                    Program.nerCountActor.Tell(sent);

                    Self.Tell(new processedUSAToday() { id = item.id });
                }
            });


            Receive<processedUSAToday>(r =>
            {
                var item = items.First(z => z.id == r.id);

                Program.cdb.UpsertDocument(item, "newsfeed").Wait();
                items.Remove(item);

                Console.WriteLine("=========> USAToday saving " + item.siteSection);

                Program.wordCountActor.Tell(new NLPHelper.CountRequest()
                {
                    Feed = "USAToday",
                    Id = item.id,
                    LineOFtext = item.stemmedDescription
                });

                Program.nerActor.Tell(new SharedMessages.NERRequest()
                {
                    id = item.id,
                    feed = "USAToday",
                    linesToProcess = new List<string>()
                    {
                        item.title,
                        item.description
                    }
                });

                Context.Parent.Tell(new processedUSAToday());
            });
        }
    }
}
