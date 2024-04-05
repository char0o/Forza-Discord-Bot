using System;
using System.Threading.Tasks;
using Discord.Interactions;
using System.Threading.Channels;

namespace DiscordBot
{
    class Program
    {
        public static void Main(string[] args)
        {
            using (var db = new RaceTimeContext())
            {
                db.Database.EnsureCreated();
            }
            new Bot().MainAsync().GetAwaiter().GetResult();
        }
    }
}