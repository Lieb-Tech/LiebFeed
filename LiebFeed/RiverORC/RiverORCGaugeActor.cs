using Akka.Actor;
using Microsoft.Azure.Documents.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;

namespace LiebFeed.RiverORC
{
    public class RiverORCGaugeActor : ReceiveActor
    {
        public RiverORCGaugeActor()
        {
            Receive<RiverORCLookup>(r =>
            {
                var qry = Program.cdb.GetDocumentQuery<RiverGaugeInfo>("riverorc").Where(z => z.id == "gauge:" + r.gauge);
                var result = qry.ToList();
                if (result.Any() && result.First().gauge == null)
                    result.Clear();

                if (result.Any())
                {
                    Sender.Tell(result.First());
                }
                else
                {
                    string html;
                    var url = "https://water.weather.gov/ahps2/hydrograph.php?wfo=bmx&gage=" + r.gauge;

                    WebClient wc = new WebClient();
                    wc.Headers.Add(HttpRequestHeader.Host, "water.weather.gov");
                    wc.Headers.Add(HttpRequestHeader.Pragma, "no-cache");
                    wc.Headers.Add(HttpRequestHeader.CacheControl, "no-cache");
                    wc.Headers.Add(HttpRequestHeader.UserAgent, "Win10");
                    wc.Headers.Add(HttpRequestHeader.Accept, "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,image/apng,*/*;q=0.8");

                    try
                    {
                        html = wc.DownloadString(url);

                        var end = html.IndexOf("Horizontal Datum");
                        var start = html.LastIndexOf("Latitude", end);
                        var latlong = html.Substring(start, end - start);
                        double lat = 0;
                        double lon = 0;

                        start = latlong.IndexOf(": ");
                        end = latlong.IndexOf("&");
                        lat = double.Parse(latlong.Substring(start + 1, end - start - 1));

                        start = latlong.IndexOf(": ", end);
                        end = latlong.IndexOf("&", start);
                        lon = -1 * double.Parse(latlong.Substring(start + 1, end - start - 1));

                        var info = new RiverGaugeInfo()
                        {
                            gauge = r.gauge,
                            id = "gauge:" + r.gauge,
                            partionKey = r.state,
                            point = new Microsoft.Azure.Documents.Spatial.Point(lon, lat)
                        };

                        Program.cdb.UpsertDocument(info, "riverorc").Wait();

                        Sender.Tell(info);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Gauge detail error: " + ex.Message);

                        Sender.Tell(new RiverGaugeInfo() { gauge = r.gauge });
                    }
                }
            });
        }
    }

    internal class RiverORCLookup
    {
        public string gauge { get; set; }
        public string state { get; set; }
    }
}
