using System;
using System.Collections.Generic;
using System.Text;

namespace LiebFeed.Helpers
{
    public static class TextDeescaper
    {
        public static string DeEscape(this string value)
        {
            return value.Replace("&lsquo;", "\"")
                .Replace("&rsquo;", "\"")
                .Replace("&apos;", "'")
                .Replace(",&nbsp;", " ");
        }
    }
}
