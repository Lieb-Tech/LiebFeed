using System;
using System.Xml.Serialization;
using System.Collections.Generic;

namespace LiebFeed.Police.Seattle
{
    public class SeattlePDItem
    {
        public string id;
        public string partionKey;

        public List<string> category;
        public string creator;
        public string description;
        public string link;
        public string title;
        public DateTimeOffset pubDate;
        public string originalXML;
    }

    /* 
     Licensed under the Apache License, Version 2.0
     http://www.apache.org/licenses/LICENSE-2.0
     */
    [XmlRoot(ElementName = "creator", Namespace = "http://purl.org/dc/elements/1.1/")]
    public class Creator
    {
        [XmlAttribute(AttributeName = "dc", Namespace = "http://www.w3.org/2000/xmlns/")]
        public string Dc { get; set; }
        [XmlText]
        public string Text { get; set; }
    }

    [XmlRoot(ElementName = "guid")]
    public class Guid
    {
        [XmlAttribute(AttributeName = "isPermaLink")]
        public string IsPermaLink { get; set; }
        [XmlText]
        public string Text { get; set; }
    }

    [XmlRoot(ElementName = "encoded", Namespace = "http://purl.org/rss/1.0/modules/content/")]
    public class Encoded
    {
        [XmlAttribute(AttributeName = "content", Namespace = "http://www.w3.org/2000/xmlns/")]
        public string Content { get; set; }
        [XmlText]
        public string Text { get; set; }
    }

    [XmlRoot(ElementName = "item")]
    public class Item
    {
        [XmlElement(ElementName = "title")]
        public string Title { get; set; }
        [XmlElement(ElementName = "link")]
        public string Link { get; set; }
        [XmlElement(ElementName = "pubDate")]
        public string PubDate { get; set; }
        [XmlElement(ElementName = "creator", Namespace = "http://purl.org/dc/elements/1.1/")]
        public Creator Creator { get; set; }
        [XmlElement(ElementName = "category")]
        public List<string> Category { get; set; }
        [XmlElement(ElementName = "guid")]
        public Guid Guid { get; set; }
        [XmlElement(ElementName = "description")]
        public string Description { get; set; }
        [XmlElement(ElementName = "encoded", Namespace = "http://purl.org/rss/1.0/modules/content/")]
        public Encoded Encoded { get; set; }
        
    }
}