using System;
using System.Collections.Generic;
using System.Text;

namespace LiebFeed
{
    public class GeoJSON_Point
    {
        public string type
        {
            get
            {
                return "Point";
            }
        }
        
        public List<float> coordinates;
    }

    public class GeoJSON_Polygon
    {
        public string type
        {
            get
            {
                return "Polygon";
            }
        }
        public List<List<float>> coordinates;
    }
}
