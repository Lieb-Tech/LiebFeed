using System;
using System.Collections.Generic;

namespace SharedMessages
{
    public class NERRequest
    {
        public string id;
        public string feed;
        public string section;
        public List<string> linesToProcess = new List<string>();
    }
    public class NERResponse
    {
        public string id;
        public string feed;
        public string section;
        public List<List<string>> results = new List<List<string>>();
    }

    public class SentimentRequest
    {
        public string id;
        public string feed;
        public string section;
        public List<string> linesToProcess = new List<string>();
    }
    public class SentimentResponse
    {
        public string id;
        public string feed;
        public string section;
        public List<int> results = new List<int>();
    }
}
