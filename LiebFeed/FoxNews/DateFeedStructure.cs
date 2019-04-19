using System;
using System.Collections.Generic;
using System.Text;

namespace LiebFeed.FoxNews
{
    public class FoxNewsItem
    {
        public string id { get; internal set; }
        public string partionKey { get; internal set; }

        public string siteSection { get; internal set; }

        public DateTimeOffset pubDate { get; internal set; }
        public string link { get; internal set; }
        public string title { get; internal set; }
        public string description { get; internal set; }

        public string origXML { get; internal set; }
        
        public string swTitle { get; internal set; }
        public string swDescription { get; internal set; }

        public string stemmedTitle { get; internal set; }
        public string stemmedDescription { get; internal set; }

        public int sentiDescription { get; internal set; }
        public int sentTitle { get; internal set; }

        public List<string> nerTitle { get; internal set; }
        public List<string> nerDescription { get; internal set; }
    }
}
