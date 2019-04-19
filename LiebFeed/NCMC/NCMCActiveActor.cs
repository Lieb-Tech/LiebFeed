using Akka.Actor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LiebFeed.NCMC
{
    public class NCMCActiveActor : ReceiveActor
    {
        public NCMCActiveActor()
        {
            Receive<processActive>(r =>
            {
                var actives = Program.cdb.GetDocumentQuery<NCMCActive>("ncmc")
                .Where(w => w.id == "active")
                .ToList();

                if (actives.Any())
                {

                }
                else
                {
                    var act = new NCMCActive()
                    {
                        id = "active",
                        partionKey = "active",
                        active = new List<NCMCActiveItem>()
                        {

                        }
                    };
                }
            });
        }
    }
}
