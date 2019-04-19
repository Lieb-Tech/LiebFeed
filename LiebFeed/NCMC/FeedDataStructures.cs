using System;
using System.Collections.Generic;
using System.Text;

namespace LiebFeed.NCMC
{
    public class NCMCActive
    {
        public string id = "active";
        public string partionKey = "active";

        public List<NCMCActiveItem> active = new List<NCMCActiveItem>();
    }

    public class NCMCActiveItem
    {
        public string id;
        public string partionKey;

        public string title;
        public string age;
        public string missing;
    }

    public class NCMCItem
    {
        public string id;
        public string partionKey;

        public string title;
        public string pubDate;
        public string link;
        public string description;
        public string img;

        public string name;
        public string age;

        public string missing;
        public string from;
        public string contact;
        public string phone;
        public string state;

        public string originalXML;
    }
}
