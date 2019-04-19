using Akka.Actor;
using Akka.Configuration;
using Akka.Routing;
using edu.stanford.nlp.ie.crf;
using LiebFeed.USGS;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace LiebFeed
{
    class Program
    {
        public static CosmosDB cdb = new CosmosDB();
        public static bool forceSave = false;

        public static IActorRef stopActor;
        public static IActorRef stemActor;
        public static IActorRef nerActor;
        public static IActorRef wordCountActor;
        public static IActorRef nerCountActor;

        static void Main(string[] args)
        {
            Console.WriteLine("Openning database");
            cdb.OpenConnection();
            Console.WriteLine("Setting up actors");

            var hocon = ConfigurationFactory.ParseString(@"akka {
    actor {
        provider = remote
    }

    remote {
        dot-netty.tcp {
            port = 0
            hostname = localhost
        }
    }
}");
        
            using (var system = ActorSystem.Create("feed-system", hocon))
            {
                nerActor = system.ActorOf<NLPHelper.NERActor>("ner");
                wordCountActor = system.ActorOf<NLPHelper.WordCountingActor>("wordCount");
                nerCountActor = system.ActorOf<NLPHelper.NERCountingActor>("nerCount");
                stopActor = system.ActorOf<NLPHelper.StopwordActor>("stopWord");
                stemActor = system.ActorOf<NLPHelper.StemmingActor>("stemActor");

                /*
                var actor1 = system.ActorOf<USGSFeedActor>();                
                actor1.Tell(new processFeedMessage() { path = "all_week.atom" } );

                var actor2 = system.ActorOf<InciWeb.InciWebFeedActor>();
                // actor2.Tell(new InciWeb.processInciWeb());
                
                var actor3 = system.ActorOf<NCMC.NCMCFeedActor>();
                actor3.Tell(new NCMC.processFeedMessage());                

                var actor4 = system.ActorOf<CSPC.CPSCFeedActor>();
                // actor4.Tell(new CSPC.processFeedMessage());

                var actor5 = system.ActorOf<WeatherGov.WeatherGovFeedActor>();
               
                // var actor6 = system.ActorOf<Police.SeattlePDFeedActor>();
                // actor6.Tell(new Police.ProcessFeed());                               

                // var actor7 = system.ActorOf<SpotCrime.DiscoverFeedActor>();
                // actor7.Tell(new SpotCrime.processDiscover());
                */

                var actor8 = system.ActorOf<RiverORC.RiverORCFeedActor>("riverFeed");
                actor8.Tell(new RiverORC.processFeedMessage());

                var actor9 = system.ActorOf<CNNUS.CNNFeedActor>("cnn");
                actor9.Tell(new CNNUS.processCNN());

                var actor10 = system.ActorOf<Reuters.ReutersFeedActor>("reuters");
                actor10.Tell(new Reuters.processReuters());

                var actor11 = system.ActorOf<FoxNews.FoxFeedActor>("fox");
                actor11.Tell(new FoxNews.processFox());

                var actor12 = system.ActorOf<USAToday.USATodayFeedActor>("usat");
                actor12.Tell(new USAToday.processUSAToday());

                var actor13 = system.ActorOf<NewsFeeds.GenericFeedActor>("gen");
                actor13.Tell(new NewsFeeds.processFeed() );

                List<string> cbseed = new List<string>()
                {
                    "https://www.cbsnews.com/latest/rss/main",
                    "https://www.cbsnews.com/latest/rss/us",
                    "https://www.cbsnews.com/latest/rss/politics",
                    "https://www.cbsnews.com/latest/rss/world",
                    "https://www.cbsnews.com/latest/rss/technology",
                };

                List<string> bbcFeeds = new List<string>() {
                    "http://feeds.bbci.co.uk/news/rss.xml",
                    "http://feeds.bbci.co.uk/news/world/rss.xml",
                    "http://feeds.bbci.co.uk/news/uk/rss.xml",
                    "http://feeds.bbci.co.uk/news/politics/rss.xml",
                    "http://feeds.bbci.co.uk/news/technology/rss.xml",
                    "http://feeds.bbci.co.uk/news/world/us_and_canada/rss.xml"
                };
               

                // Exit the system after ENTER is pressed
                Console.ReadLine();
            }
        }
    }
}
