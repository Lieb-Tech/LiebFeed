using System.Xml.Serialization;
using System.Collections.Generic;
using System;

namespace LiebFeed.WeatherGov
{
    public class WeatherGovActive
    {
        public string id { get { return "active"; } }
        public string partionKey { get { return "active"; } }

        public List<WeatherGovActiveItem> active = new List<WeatherGovActiveItem>();
    }

    public class WeatherGovActiveItem
    {
        public string id { get; set; }
        public string partionKey { get; set; }

        public DateTimeOffset updated { get; set; }
        public string title { get; set; }
        public string areaDesc { get; set; }
    }

    public class WeatherGovItem
    {
        public string id { get; set; }
        public string partionKey { get; set; }

        public DateTimeOffset updated { get; set; }
        public DateTimeOffset published { get; set; }
        public DateTimeOffset expires { get; set; }

        public string title { get; set; }
        public string link { get; set; }
        public string summary { get; set; }
        public string eventType { get; set; }
        public string effective { get; set; }
        public string status { get; set; }
        public string msgType { get; set; }
        public string category { get; set; }
        public string urgency { get; set; }
        public string severity { get; set; }
        public string certainty { get; set; }
        public string areaDesc { get; set; }
        public Microsoft.Azure.Documents.Spatial.Polygon polygon { get; set; }
        public Geocode geocode { get; set; }
        public VTEC parameter { get; set; }
        public string originalXML { get; set; }
    }

    public class VTEC
    {
        public string valueType { get; set; }
        public string vtecValue { get; set; }
    }

    /* 
 Licensed under the Apache License, Version 2.0

 http://www.apache.org/licenses/LICENSE-2.0
 */
    [XmlRoot(ElementName = "author", Namespace = "http://www.w3.org/2005/Atom")]
    public class Author
    {
        [XmlElement(ElementName = "name", Namespace = "http://www.w3.org/2005/Atom")]
        public string Name { get; set; }
    }

    [XmlRoot(ElementName = "link", Namespace = "http://www.w3.org/2005/Atom")]
    public class Link
    {
        [XmlAttribute(AttributeName = "href")]
        public string Href { get; set; }
    }

    [XmlRoot(ElementName = "geocode", Namespace = "urn:oasis:names:tc:emergency:cap:1.1")]
    public class Geocode
    {
        [XmlElement(ElementName = "valueName", Namespace = "http://www.w3.org/2005/Atom")]
        public List<string> ValueName { get; set; }
        [XmlElement(ElementName = "value", Namespace = "http://www.w3.org/2005/Atom")]
        public List<string> Value { get; set; }
    }

    [XmlRoot(ElementName = "parameter", Namespace = "urn:oasis:names:tc:emergency:cap:1.1")]
    public class Parameter
    {
        [XmlElement(ElementName = "valueName", Namespace = "http://www.w3.org/2005/Atom")]
        public string ValueName { get; set; }
        [XmlElement(ElementName = "value", Namespace = "http://www.w3.org/2005/Atom")]
        public string Value { get; set; }
    }

    [XmlRoot(ElementName = "entry", Namespace = "http://www.w3.org/2005/Atom")]
    public class Entry
    {
        [XmlElement(ElementName = "id", Namespace = "http://www.w3.org/2005/Atom")]
        public string Id { get; set; }
        [XmlElement(ElementName = "updated", Namespace = "http://www.w3.org/2005/Atom")]
        public string Updated { get; set; }
        [XmlElement(ElementName = "published", Namespace = "http://www.w3.org/2005/Atom")]
        public string Published { get; set; }
        [XmlElement(ElementName = "author", Namespace = "http://www.w3.org/2005/Atom")]
        public Author Author { get; set; }
        [XmlElement(ElementName = "title", Namespace = "http://www.w3.org/2005/Atom")]
        public string Title { get; set; }
        [XmlElement(ElementName = "link", Namespace = "http://www.w3.org/2005/Atom")]
        public Link Link { get; set; }
        [XmlElement(ElementName = "summary", Namespace = "http://www.w3.org/2005/Atom")]
        public string Summary { get; set; }
        [XmlElement(ElementName = "event", Namespace = "urn:oasis:names:tc:emergency:cap:1.1")]
        public string Event { get; set; }
        [XmlElement(ElementName = "effective", Namespace = "urn:oasis:names:tc:emergency:cap:1.1")]
        public string Effective { get; set; }
        [XmlElement(ElementName = "expires", Namespace = "urn:oasis:names:tc:emergency:cap:1.1")]
        public string Expires { get; set; }
        [XmlElement(ElementName = "status", Namespace = "urn:oasis:names:tc:emergency:cap:1.1")]
        public string Status { get; set; }
        [XmlElement(ElementName = "msgType", Namespace = "urn:oasis:names:tc:emergency:cap:1.1")]
        public string MsgType { get; set; }
        [XmlElement(ElementName = "category", Namespace = "urn:oasis:names:tc:emergency:cap:1.1")]
        public string Category { get; set; }
        [XmlElement(ElementName = "urgency", Namespace = "urn:oasis:names:tc:emergency:cap:1.1")]
        public string Urgency { get; set; }
        [XmlElement(ElementName = "severity", Namespace = "urn:oasis:names:tc:emergency:cap:1.1")]
        public string Severity { get; set; }
        [XmlElement(ElementName = "certainty", Namespace = "urn:oasis:names:tc:emergency:cap:1.1")]
        public string Certainty { get; set; }
        [XmlElement(ElementName = "areaDesc", Namespace = "urn:oasis:names:tc:emergency:cap:1.1")]
        public string AreaDesc { get; set; }
        [XmlElement(ElementName = "polygon", Namespace = "urn:oasis:names:tc:emergency:cap:1.1")]
        public string Polygon { get; set; }
        [XmlElement(ElementName = "geocode", Namespace = "urn:oasis:names:tc:emergency:cap:1.1")]
        public Geocode Geocode { get; set; }
        [XmlElement(ElementName = "parameter", Namespace = "urn:oasis:names:tc:emergency:cap:1.1")]
        public Parameter Parameter { get; set; }
    }

    [XmlRoot(ElementName = "feed", Namespace = "http://www.w3.org/2005/Atom")]
    public class Feed
    {
        [XmlElement(ElementName = "id", Namespace = "http://www.w3.org/2005/Atom")]
        public string Id { get; set; }
        [XmlElement(ElementName = "logo", Namespace = "http://www.w3.org/2005/Atom")]
        public string Logo { get; set; }
        [XmlElement(ElementName = "generator", Namespace = "http://www.w3.org/2005/Atom")]
        public string Generator { get; set; }
        [XmlElement(ElementName = "updated", Namespace = "http://www.w3.org/2005/Atom")]
        public string Updated { get; set; }
        [XmlElement(ElementName = "author", Namespace = "http://www.w3.org/2005/Atom")]
        public Author Author { get; set; }
        [XmlElement(ElementName = "title", Namespace = "http://www.w3.org/2005/Atom")]
        public string Title { get; set; }
        [XmlElement(ElementName = "link", Namespace = "http://www.w3.org/2005/Atom")]
        public Link Link { get; set; }
        [XmlElement(ElementName = "entry", Namespace = "http://www.w3.org/2005/Atom")]
        public List<Entry> Entry { get; set; }
        [XmlAttribute(AttributeName = "xmlns")]
        public string Xmlns { get; set; }
        [XmlAttribute(AttributeName = "cap", Namespace = "http://www.w3.org/2000/xmlns/")]
        public string Cap { get; set; }
        [XmlAttribute(AttributeName = "ha", Namespace = "http://www.w3.org/2000/xmlns/")]
        public string Ha { get; set; }
    }
}