using System;
using System.Collections.Generic;
using System.Text;

namespace LiebFeed.Helpers
{
    class GeneralHelper
    {
        public static string IdHelper(string value)
        {
            return value.Replace("/", "")
                .Replace("?", "")
                .Replace("=", "")
                .Replace("-", "")
                .Replace(".", "")
                .Replace(":", "")
                .Replace("#", "");
        }
    }
}
