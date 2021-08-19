using System;
using System.Collections.Generic;

namespace Replays.Messages
{
    public class WebMessage
    {
        public string message { get; set; }
        public string data { get; set; }
    }

    public class RetrieveVideos
    {
        public string game { get; set; }
        public string sortBy { get; set; }
    }

    public class VideoList
    {
        public string game { get; set; }
        public List<string> games { get; set; }
        public string sortBy { get; set; }
        public List<Video> sessions { get; set; }
        public long sessionsSize { get; set; }
        public List<Video> clips { get; set; }
        public long clipsSize { get; set; }
    }
    public class Video
    {
        public DateTime date { get; set; }
        public string type { get; set; }
        public long size { get; set; }
        public string game { get; set; }
        public string fileName { get; set; }
        public string thumbnail { get; set; }
    }
}