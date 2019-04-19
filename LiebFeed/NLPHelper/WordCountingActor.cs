using Akka.Actor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LiebFeed.NLPHelper
{
    class WordCountingActor : ReceiveActor
    {
        Dictionary<string, List<CountWord>> words = new Dictionary<string, List<CountWord>>();

        string lastKey = "";
        public WordCountingActor()
        {
            Receive<CountRequest>(r =>
            {
                string key = DateTimeOffset.UtcNow.ToString("yyyy-MM-dd-hh-mm");

                if (!words.ContainsKey(key))                                    
                    words.Add(key, new List<CountWord>());                                   

                var current = words[key];
                // check if the document has been processed yet
                var inThere = words.SelectMany(a => a.Value).Any(z => z.data.Any(x => x.Id == r.Id && x.Feed == r.Id));
                if (!inThere)
                {                                        
                    // not yet, so add words 
                    foreach (var w in r.LineOFtext.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                    {
                        var cleaned = w.Replace("(", "").Replace(")", "").Replace("-", "").Replace(":", "").Trim().ToLower();
                        var word = current.FirstOrDefault(a => a.word == cleaned);
                        if (word != null)
                        {                            
                            word.data.Add(new CountWordItem()
                            {
                                Added = DateTimeOffset.UtcNow,
                                Feed = r.Feed,
                                Id = r.Id
                            });
                        }
                        else
                        {
                            current.Add(new CountWord()
                            {
                                word = cleaned,
                                data = new List<CountWordItem>() {
                                    new CountWordItem()
                                    {
                                        Added = DateTimeOffset.UtcNow,
                                        Feed = r.Feed,
                                        Id = r.Id
                                    }
                                }
                            });
                        }
                    }
                    if (key != lastKey)
                        Program.cdb.UpsertDocument(new CountDocument() { id = key, partionKey = key, words = current }, "system").Wait();

                    lastKey = key;
                }
            });
        }
    }   

    internal class CountDocument
    {
        public string id;
        public string partionKey;        
        public List<CountWord> words = new List<CountWord>();
    }

    internal class CountWordItem
    {
        public DateTimeOffset Added;
        public string Id;
        public string Feed;
    }

    internal class CountWord
    {
        public string word;
        public List<CountWordItem> data = new List<CountWordItem>();
    }

    internal class CountRequest
    {
        public string LineOFtext;
        public string Id;
        public string Feed;
    }
}
