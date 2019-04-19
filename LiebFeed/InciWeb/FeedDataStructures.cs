using Microsoft.Azure.Documents.Spatial;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace LiebFeed.InciWeb
{
    public class InciWebActiveItem
    {
        public string id;
        public string partionKey;

        public string title;
        public DateTimeOffset pubDate;
    }

    public class InciWebActive
    {
        public string id = "active";
        public string partionKey = "active";

        public List<InciWebActiveItem> updates = new List<InciWebActiveItem>();
    }    

    public class InciWebItem
    {        
        public string id;
        public string partionKey;

        public string title;
        public DateTimeOffset pubDate;
        public DateTimeOffset published;
        public string link;
        public string description;

        public Point point;
        public string lat;
        public string lng;

        public string incidentType;
        public string outlook;
        public string size;

        public string originalXML;
    }
}
