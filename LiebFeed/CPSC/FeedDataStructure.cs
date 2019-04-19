using System;
using System.Collections.Generic;
using System.Text;

namespace LiebFeed.CSPC
{
    public class CPSCActive
    {
        public string id = "active";
        public string partionKey = "active";

        public List<CPSCActiveItem> active = new List<CPSCActiveItem>();
    }
    public class CPSCActiveItem
    {
        public string id;
        public string partionKey;

        public string title;
        public string link;
    }

    public class CPSCItem
    {
        public string id;
        public string partionKey;

        public string title;
        public string link;
        public string description;
        public string pubDate;
        public string imgSrc;
        public string fullDescription;
        public string remedy;
        public string manufacture;
        public string soldAt;
        public string hazard;

        public string originalXML;
    }
}
