using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Discord_Bot
{
    public static class TrackService
    {
        public static List<Track> Tracks { get; set; }
        static TrackService()
        {
            Tracks = new List<Track>();
            Tracks.Add(new Track("Maple Valley", "maplevalley"));
            Tracks.Add(new Track("Suzuka", "suzuka"));
            Tracks.Add(new Track("Lime Rock", "limerock"));
            Tracks.Add(new Track("Spa-Francorchamps", "spa"));
        }
    }
}
