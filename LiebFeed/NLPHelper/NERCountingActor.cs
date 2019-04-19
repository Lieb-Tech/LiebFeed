using Akka.Actor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LiebFeed.NLPHelper
{
    class NERCountingActor : ReceiveActor
    {
        List<string> ids = new List<string>();
        Dictionary<string, List<CountWord>> words = new Dictionary<string, List<CountWord>>();
        string lastKey = "";

        public NERCountingActor()
        {
            Receive<SharedMessages.NERResponse>(r =>
            {
                // group values by the minute
                string key = DateTimeOffset.UtcNow.ToString("yyyy-MM-dd-hh-mm");

                if (!ids.Contains(r.id))
                {
                    ids.Add(r.id);
                    if (!words.ContainsKey(key))
                        words.Add(key, new List<CountWord>());

                    foreach (var result in r.results)
                    {
                        foreach (var word in result)
                        {
                            if (!words[key].Any(a => a.word == word))
                            {
                                words[key].Add(new CountWord()
                                {
                                    word = word,
                                    data = new List<CountWordItem>()
                                {
                                      new CountWordItem()
                                      {
                                           Added = DateTimeOffset.UtcNow,
                                           Feed = r.feed,
                                           Id = r.id
                                      }
                                }
                                });
                            }
                            else
                            {
                                words[key].Add(new CountWord()
                                {
                                    word = word,
                                    data = new List<CountWordItem>()
                                {
                                      new CountWordItem()
                                      {
                                           Added = DateTimeOffset.UtcNow,
                                           Feed = r.feed,
                                           Id = r.id
                                      }
                                }
                                });
                            }
                        }
                    }
                    if (key != lastKey)
                        Program.cdb.UpsertDocument(new CountDocument() { id = "NER:" + key, partionKey = key, words = words[key] }, "system").Wait();
                    lastKey = key;
                }
            });
        }
    }   
}
