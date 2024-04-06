namespace DiscordBot
{
    public class RaceTime
    {
        public int Id { get; set; }
        public string TrackName { get; set; }
        public string Time { get; set; }
        public ulong UserID { get; set; }
        public string CarClass { get; set; }
    }
}