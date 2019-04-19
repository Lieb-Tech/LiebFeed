using Akka.Actor;
using Akka.Routing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Xml.Linq;

namespace LiebFeed.RiverORC
{
    public class RiverORCFeedActor : ReceiveActor
    {
        public RiverORCFeedActor()
        {
            var props = Props.Create<RiverORCStateActor>(); //.WithRouter(new RoundRobinPool(5));
            var actor = Context.ActorOf(props, "riverState");

            int toProcess = 0;
            int processed = 0;

            string[] states = new string[52]
                {
                    "al",
                    "ar",
                    "ak",
                    "az",

                    "ca",
                    "co",
                    "ct",

                    "de",
                    "dc",

                    "fl",

                    "ga",

                    "hi",

                    "id",
                    "ia",
                    "il",
                    "in",

                    "ks",
                    "ky",

                    "la",

                    "ma",
                    "mn",
                    "md",
                    "mi",
                    "ms",
                    "ms",
                    "mo",
                    "me",

                    "ne",
                    "ny",
                    "nj",
                    "nv",
                    "nm",
                    "nh",
                    "nc",
                    "nd",

                    "or",
                    "oh",
                    "ok",

                    "pa",
                    "pr",

                    "ri",

                    "sc",
                    "sd",

                    "tn",
                    "tx",

                    "ut",

                    "vt",
                    "va",
                    // "vi",

                    "wa",
                    "wv",
                    "wi",
                    "wy"
                };

            /*Context.System.Scheduler.ScheduleTellRepeatedly(TimeSpan.FromSeconds(5),
                        TimeSpan.FromMinutes(5), Self,
                        new processFeedMessage(), Self);            
                        */
            Receive<processedORC>(e =>
            {
                processed++;                
                if (processed == states.Count())
                {
                    Console.WriteLine("Finished processing RiverORC");
                }
                else
                {
                    var next = states.Skip(processed).First();
                    actor.Tell(new processStateMessage() { state = next });
                }
            });

            Receive<processFeedMessage>(m =>
            {
                processed = 0;
                toProcess = 0;

                var next = states.Skip(processed).First();
                actor.Tell(new processStateMessage() { state = next });                
            });
        }
    }

    internal class processStateMessage
    {
        public string state { get; set; }
    }

    internal class ProcessORCItem
    {
        public ProcessORCItem()
        {
        }

        public string state { get; set; }
        public XElement item { get; set; }
    }

    internal class processFeedMessage
    {
    }

    internal class processedORC
    {
        internal RiverORCItem item;
    }
}