using Akka.Actor;
using LiebFeed.USGS.FeedDataStructures;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LiebFeed.USGS
{
    public class USGSRecentActor : ReceiveActor
    {
        USGSRecent recent60;
        USGSRecent recent120;

        protected override void PreStart()
        {
            base.PreStart();

            var recents = Program.cdb.GetDocumentQuery<USGSRecent>("usgs")
                .Where(z => z.id == "recent60" || z.id == "recent120")
                .ToList();

            if (!recents.Any(z => z.id == "recent60"))
            {
                recent60 = new USGSRecent() { id = "recent60" };
                Program.cdb.UpsertDocument(recent60, "usgs").Wait();
            }
            else
                recent60 = recents.First(z => z.id == "recent60");

            if (!recents.Any(z => z.id == "recent60"))
            {
                recent120 = new USGSRecent() { id = "recent120" };
                Program.cdb.UpsertDocument(recent120, "usgs").Wait();
            }
            else
                recent120 = recents.First(z => z.id == "recent120");
        }

        public USGSRecentActor()
        {
            Context.System.Scheduler.ScheduleTellRepeatedly(TimeSpan.FromSeconds(15), TimeSpan.FromMinutes(1), Self, new pruneRecent(), Self);

            Receive<processRecent>(r =>
            {
                var now = DateTimeOffset.Now;

                if (!recent120.items.Any(z => z.id == r.Item.id))
                {
                    if ((DateTimeOffset.Now - r.Item.updated).TotalHours <= 2)

                    {
                        recent120.items.Add(new USGSRecentItem()
                        {
                            id = r.Item.id,
                            title = r.Item.title,
                            updated = r.Item.updated
                        });
                        Program.cdb.UpsertDocument(recent120, "usgs").Wait();
                    }
                }

                if (!recent60.items.Any(z => z.id == r.Item.id))
                { 
                    if ((now - r.Item.updated).TotalHours <= 1)
                    {
                        recent60.items.Add(new USGSRecentItem()
                        {
                            id = r.Item.id,
                            title = r.Item.title,
                            updated = r.Item.updated
                        });
                        Program.cdb.UpsertDocument(recent60, "usgs").Wait();
                    }
                }                
            });

            Receive<pruneRecent>(r =>
            {
                var now = DateTimeOffset.Now;
                var old = recent60.items.Where(z => (now - z.updated).TotalHours > 1).ToList();
                if (old.Any())
                {
                    recent60.items.RemoveAll(z => old.Contains(z));
                    Program.cdb.UpsertDocument(recent60, "usgs").Wait();
                }

                old = recent120.items.Where(z => (now - z.updated).TotalHours > 2).ToList();
                if (old.Any())
                {
                    recent120.items.RemoveAll(z => old.Contains(z));
                    Program.cdb.UpsertDocument(recent120, "usgs").Wait();
                }
            });
        }
    }

    internal class pruneRecent
    {
    }

    internal class processRecent
    {
        public FeedDataStructures.USGSItem Item;
        public processRecent(FeedDataStructures.USGSItem item)
        {
            Item = item;
        }
    }
}
