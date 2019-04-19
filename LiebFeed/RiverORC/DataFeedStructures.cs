using System.Xml.Serialization;
using System.Collections.Generic;
using System;
using Newtonsoft.Json;

namespace LiebFeed.RiverORC
{
    public class RiverGaugeInfo
    {
        public string id { get; set; }
        public string partionKey { get; set; }

        public string gauge { get; set; }

        public Microsoft.Azure.Documents.Spatial.Point point { get; set; }
    }

    public class RiverORCItem
    {
        [JsonProperty(PropertyName = "id")]
        public string id { get; set; }
        public string partionKey { get; set; }

        public string gauge { get; set; }

        public DateTimeOffset pubDate { get; set; }

        public string title { get; set; }
        public string link { get; set; }
        public string description { get; set; }

        public string stage { get; set; }

        public double action { get; set; }
        public double minor { get; set; }
        public double major { get; set; }
        public double moderate { get; set; }

        public double latest { get; set; }
        public double deltaLatest { get; set; }

        public Microsoft.Azure.Documents.Spatial.Point point;

        public string originalXML { get; set; }
    }
}
