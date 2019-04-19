using System;
using System.Collections.Generic;
using System.Text;

namespace LiebFeed
{
    public class CommonDataFormat
    {
        public string id;
        public string partionKey;

        public string source;
        public string sourceId;
        public string sourcePk;

        public string title;
        public string extra;
        public DateTimeOffset pubDate;
        public DateTimeOffset expires;
        public Microsoft.Azure.Documents.Spatial.Geometry point;        
    }
}
