using Akka.Actor;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace LiebFeed.NLPHelper
{
    class NERActor : ReceiveActor
    {
        public NERActor()
        {
            Dictionary<string, IActorRef> nerInProcess = new Dictionary<string, IActorRef>();
            Dictionary<string, IActorRef> sentInProcess = new Dictionary<string, IActorRef>();

            Receive<SharedMessages.NERRequest>(r =>
            {
                if (!nerInProcess.ContainsKey(r.id + "+#+" + r.section))
                {
                    var remote = Context.ActorSelection("akka.tcp://nlp-system@localhost:8080/user/akka");
                    var req = JsonConvert.SerializeObject(r);
                    nerInProcess.Add(r.id + "+#+" + r.section, Sender);
                    remote.Tell(req);
                }
            });

            Receive<string>(r =>
            {
                if (r.StartsWith("ner:"))
                {
                    var resp = JsonConvert.DeserializeObject<SharedMessages.NERResponse>(r.Substring(4));
                    try
                    {
                        var actor = nerInProcess[resp.id + "+#+" + resp.section];
                        actor.Tell(resp);
                        nerInProcess.Remove(resp.id + "+#+" + resp.section);

                        Program.nerCountActor.Tell(resp);
                    }
                    catch (Exception e)
                    {
                        var s = "";

                    }
                }
                else if(r.StartsWith("sent:"))
                {
                    var resp = JsonConvert.DeserializeObject<SharedMessages.SentimentResponse>(r.Substring(5));
                    try
                    {
                        var actor = sentInProcess[resp.id + "+#+" + resp.section];
                        actor.Tell(resp);
                        sentInProcess.Remove(resp.id + "+#+" + resp.section);                        
                    }
                    catch (Exception e)
                    {
                        var s = "";

                    }
                }
            });
        }
    }
}
