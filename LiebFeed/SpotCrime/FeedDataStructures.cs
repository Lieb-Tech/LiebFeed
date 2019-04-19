using System;
using System.Collections.Generic;
using System.Text;

namespace LiebFeed.SpotCrime
{
    public class SpotCrimeItem
    {
        public string id;
        public string partionKey;

        public string title;
        public DateTimeOffset pubDate;
        public string link;
        public string description;
        public Microsoft.Azure.Documents.Spatial.Point point;
    }
}