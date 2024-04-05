using Microsoft.EntityFrameworkCore;

namespace DiscordBot
{
    public class RaceTimeContext : DbContext
    {
        public DbSet<RaceTime> RaceTimes { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
            options.UseSqlite("Data Source=racetimes.db");
        }

    }
}