using Akka.Actor;
using Microsoft.Azure.Documents.Spatial;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace LiebFeed.WeatherGov
{
    public class WeatherGovItemActor : ReceiveActor
    {
        public WeatherGovItemActor()
        {
            Receive<ProcessWeatherItem>(r =>
            {
                var entry = XMLSerializeHelper.Deserialize<Entry>(r.item.ToString());

                Microsoft.Azure.Documents.Spatial.Polygon poly = null;
                if (!string.IsNullOrWhiteSpace(entry.Polygon))
                {
                    var pnts = entry.Polygon.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    var pos = new List<Position>();
                    foreach (var p in pnts)
                    {
                        pos.Add(new Position(
                                double.Parse(p.Substring(p.IndexOf(",") + 1)),
                                double.Parse(p.Substring(0, p.IndexOf(",")))
                            ));
                    }
                    var ring = new Microsoft.Azure.Documents.Spatial.LinearRing(pos);
                    poly = new Microsoft.Azure.Documents.Spatial.Polygon(new List<LinearRing>() { ring });
                    
                }

                var item = new WeatherGovItem()
                {
                    id = entry.Id.Substring(entry.Id.IndexOf("=") + 1),                    
                    parameter = new VTEC() {
                        valueType =  entry.Parameter.ValueName,
                         vtecValue = entry.Parameter.Value
                    },
                    areaDesc = entry.AreaDesc,
                    category = entry.Category,
                    certainty = entry.Certainty,
                    effective = entry.Effective,
                    eventType = entry.Event,
                    expires = DateTimeOffset.Parse(entry.Expires),
                    geocode = entry.Geocode,
                    link = entry.Link.Href,
                    msgType = entry.MsgType,
                    polygon = poly,
                    published = DateTimeOffset.Parse(entry.Published),
                    severity = entry.Severity,
                    status = entry.Status,
                    summary = entry.Summary,
                    title = entry.Title,
                    updated = DateTimeOffset.Parse(entry.Updated),
                    urgency = entry.Urgency,
                    originalXML = r.item.ToString()
                };
                item.partionKey = item.id;

                var prior = Program.cdb.GetDocumentQuery<WeatherGovItem>("weathergov")
                    .Where(w => w.id == item.id && w.partionKey == item.id)
                    .Select(w => w.updated)
                    .ToList();

                if (Program.forceSave)
                    prior.Clear();

                // prior.Clear();

                if (prior.Any())
                {
                    if (prior.First() != item.updated)
                    {
                        // TODO: Message Archiving                       
                        Program.cdb.UpsertDocument(item, "weathergov").Wait();

                        Program.cdb.UpsertDocument(new CommonDataFormat()
                        {
                            id = Guid.NewGuid().ToString(),
                            partionKey = "weather",
                            source = "weather",
                            title = item.title,
                            extra = item.summary,
                            point = poly,
                            pubDate = item.updated,
                            expires = item.expires,
                            sourceId = item.id,
                            sourcePk = item.partionKey
                        }, "commondata").Wait();
                    }                    
                }
                else
                {
                    Program.cdb.UpsertDocument(item, "weathergov").Wait();

                    Program.cdb.UpsertDocument(new CommonDataFormat()
                    {
                        id = Guid.NewGuid().ToString(),
                        partionKey = "weather",
                        source = "weather",
                        title = item.title,
                        extra = item.summary,
                        point = poly,
                        pubDate = item.updated,
                        expires = item.expires,
                        sourceId = item.id,
                        sourcePk = item.partionKey
                    }, "commondata").Wait();
                }

                if (item.parameter.vtecValue != "")
                {
                    bool inactive = (item.parameter.vtecValue.StartsWith("/O.CAN") || item.parameter.vtecValue.StartsWith("/O.EXP"))
                        || item.expires < DateTimeOffset.UtcNow;

                    if (inactive)
                        Sender.Tell(new processedWeatherGov());
                    else
                        Sender.Tell(new processedWeatherGov() { item = item });                    
                }
                else
                    Sender.Tell(new processedWeatherGov() );

                var a = "";
            });
        }        
    }
}
