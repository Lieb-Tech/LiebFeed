using Akka.Actor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LiebFeed.NLPHelper
{
    public class StemmingActor : ReceiveActor
    {
        static EnglishStemmer stemmer = new EnglishStemmer();

        public StemmingActor()
        {            
            Receive<StemmingRequest>(r =>
            {
                var ret = new StemmingResponse() { id = r.id };
                foreach (var l in r.linesOfText)
                {
                    string results = "";
                    var words = l.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToList();
                    foreach (var w in words)
                    {
                        try
                        {
                            results += " " + stemmer.Stem(w.Trim());
                        }
                        catch (Exception ex)
                        {
                            results += " " + w.Trim();
                            var s = ex.Message;
                        }
                    }
                    ret.lines.Add(results.Trim());
                }

                Sender.Tell(ret);
            });
        }
    }

    internal class StemmingResponse
    {
        public string id;
        public List<string> lines = new List<string>();
    }

    internal class StemmingRequest
    {        
        public string id;
        public List<string> linesOfText;
    }
}
