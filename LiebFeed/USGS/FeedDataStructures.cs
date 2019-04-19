using System;
using System.Xml.Serialization;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace LiebFeed.USGS.FeedDataStructures
{
    public class USGSRecent
    {
        public string id;
        public string partionKey 
        {
            get
            {
                return id;
            }
        }

        public List<USGSRecentItem> items = new List<USGSRecentItem>();
    }
    public class USGSRecentItem
    {
        public string id;
        public DateTimeOffset updated;
        public string title;
    }

    public class USGSItemArchive
    {
        [JsonProperty("id")]
        public string id;        
        public string partionKey;

        public List<USGSItem> updates = new List<USGSItem>();
    }

    public class USGSItem
    {
        [JsonProperty("id")]
        public string id;        
        public string partionKey;

        public string source;
        public string title;
        public DateTimeOffset published;
        public DateTimeOffset updated;
        public string link;
        public string summary;
        public Microsoft.Azure.Documents.Spatial.Point point;
        public float elevation;
        public float magnitude;
        public string originalXML;        
    }
    
    /*************************/

    [XmlRoot(ElementName = "author", Namespace = "http://www.w3.org/2005/Atom")]
    public class FeedAuthor
    {
        [XmlElement(ElementName = "name", Namespace = "http://www.w3.org/2005/Atom")]
        public string Name { get; set; }
        [XmlElement(ElementName = "uri", Namespace = "http://www.w3.org/2005/Atom")]
        public string Uri { get; set; }
    }

    [XmlRoot(ElementName = "link", Namespace = "http://www.w3.org/2005/Atom")]
    public class FeedLink
    {
        [XmlAttribute(AttributeName = "rel")]
        public string Rel { get; set; }
        [XmlAttribute(AttributeName = "href")]
        public string Href { get; set; }
        [XmlAttribute(AttributeName = "type")]
        public string Type { get; set; }
    }

    [XmlRoot(ElementName = "summary", Namespace = "http://www.w3.org/2005/Atom")]
    public class FeedSummary
    {
        [XmlAttribute(AttributeName = "type")]
        public string Type { get; set; }
        [XmlText]
        public string Text { get; set; }
    }

    [XmlRoot(ElementName = "category", Namespace = "http://www.w3.org/2005/Atom")]
    public class FeedCategory
    {
        [XmlAttribute(AttributeName = "label")]
        public string Label { get; set; }
        [XmlAttribute(AttributeName = "term")]
        public string Term { get; set; }
    }

    [XmlRoot(ElementName = "entry", Namespace = "http://www.w3.org/2005/Atom")]
    public class FeedEntry
    {
        [XmlElement(ElementName = "id", Namespace = "http://www.w3.org/2005/Atom")]
        public string Id { get; set; }
        [XmlElement(ElementName = "title", Namespace = "http://www.w3.org/2005/Atom")]
        public string Title { get; set; }
        [XmlElement(ElementName = "updated", Namespace = "http://www.w3.org/2005/Atom")]
        public string Updated { get; set; }
        [XmlElement(ElementName = "link", Namespace = "http://www.w3.org/2005/Atom")]
        public FeedLink Link { get; set; }
        [XmlElement(ElementName = "summary", Namespace = "http://www.w3.org/2005/Atom")]
        public FeedSummary Summary { get; set; }
        [XmlElement(ElementName = "point", Namespace = "http://www.georss.org/georss")]
        public string Point { get; set; }
        [XmlElement(ElementName = "elev", Namespace = "http://www.georss.org/georss")]
        public string Elev { get; set; }
        [XmlElement(ElementName = "category", Namespace = "http://www.w3.org/2005/Atom")]
        public List<FeedCategory> Category { get; set; }
    }

    [XmlRoot(ElementName = "feed", Namespace = "http://www.w3.org/2005/Atom")]
    public class FeedFeed
    {
        [XmlElement(ElementName = "title", Namespace = "http://www.w3.org/2005/Atom")]
        public string Title { get; set; }
        [XmlElement(ElementName = "updated", Namespace = "http://www.w3.org/2005/Atom")]
        public string Updated { get; set; }
        [XmlElement(ElementName = "author", Namespace = "http://www.w3.org/2005/Atom")]
        public FeedAuthor Author { get; set; }
        [XmlElement(ElementName = "id", Namespace = "http://www.w3.org/2005/Atom")]
        public string Id { get; set; }
        [XmlElement(ElementName = "link", Namespace = "http://www.w3.org/2005/Atom")]
        public FeedLink Link { get; set; }
        [XmlElement(ElementName = "icon", Namespace = "http://www.w3.org/2005/Atom")]
        public string Icon { get; set; }
        [XmlElement(ElementName = "entry", Namespace = "http://www.w3.org/2005/Atom")]
        public List<FeedEntry> Entry { get; set; }
        [XmlAttribute(AttributeName = "xmlns")]
        public string Xmlns { get; set; }
        [XmlAttribute(AttributeName = "georss", Namespace = "http://www.w3.org/2000/xmlns/")]
        public string Georss { get; set; }
    }
}