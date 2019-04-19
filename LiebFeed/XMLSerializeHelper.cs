using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace LiebFeed
{
    class XMLSerializeHelper
    {
        public static T Deserialize<T>(string input) where T : class
        {
            System.Xml.Serialization.XmlSerializer ser = new System.Xml.Serialization.XmlSerializer(typeof(T));

            using (StringReader sr = new StringReader(input))
            {
                return (T)ser.Deserialize(sr);
            }
        }
    }
}
