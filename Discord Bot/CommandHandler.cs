using Discord.Commands;
using Discord.WebSocket;
using Discord_Bot;
using System.Data.SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Threading.Channels;
using Discord.Interactions;
using System.Formats.Asn1;

namespace DiscordBot
{
    public class CommandHandler
    {
        private readonly DiscordSocketClient _client;
        private readonly CommandService _commands;
        private readonly Dictionary<string, Func<SocketCommandContext, Task>> _commandHandler;
        private readonly DatabaseManager _dbManager;
        public CommandHandler(DiscordSocketClient client, CommandService commands)
        {
            _client = client;
            _commands = commands;
            _commandHandler = new Dictionary<string, Func<SocketCommandContext, Task>>();
            _dbManager = new DatabaseManager("Data Source=racetimes.db");

            _commandHandler.Add("addtime", AddRaceTimeCommand);
            _commandHandler.Add("tracks", ShowTracksCommand);
            _commandHandler.Add("besttimes", BestTimesCommand);
            _commandHandler.Add("cleartime", ClearTimeCommand);
            _commandHandler.Add("leaderboard", LeaderBoardCommand);
            _client.MessageReceived += HandleCommandAsync;
        }

        private async Task LeaderBoardCommand(SocketCommandContext context)
        {
            string channelName = context.Channel.Name;
            if (!channelName.Contains("class"))
            {
                return;
            }
            string carClass = channelName.Substring(channelName.Length - 1);
            string[] args = context.Message.Content.Split();
            if (args.Length < 2)
            {
                await context.Channel.SendMessageAsync("Invalid usage. Try !leaderboard TRACKNAME");
                return;
            }
            string trackName = args[1].ToLower();
            Track track = TrackService.Tracks.FirstOrDefault(trackReference => trackReference.Reference == trackName);
            if (track == null)
            {
                await context.Channel.SendMessageAsync($"Invalid track. Type !tracks to see the list of tracks");
                return;
            }

            string leaderboard = $"__** Leaderboard for {track.Name} Class {carClass.ToUpper()} **__ \n";

            var times = GetUserTimes(trackName, carClass);

            var bestTime = times
                .SelectMany(dict => dict)
                .GroupBy(pair => pair.Key)
                .Select(group => new
            {
                ID = group.Key,
                BestTime = group.Min(pair => pair.Value)
            })
                .OrderBy(result => result.BestTime);
            foreach (var item in bestTime)
            {
                string globalName = context.Guild.GetUser(item.ID).GlobalName;
                string formatted = item.BestTime.ToString(@"mm\:ss\.fff");
                leaderboard += $"* {formatted} - {globalName}\n";
            }
            await context.Channel.SendMessageAsync(leaderboard);
        }
        private List<Dictionary<ulong, TimeSpan>> GetUserTimes(string trackName, string carClass)
        {

            List<SQLiteParameter> parameters = new List<SQLiteParameter>
            {
                new SQLiteParameter("@TrackName", trackName),
                new SQLiteParameter("@CarClass", carClass)
            };

            List<Dictionary<string, object>> result = _dbManager.ExecuteQuery($"SELECT * FROM RaceTimes WHERE TrackName=@TrackName AND CarClass=@CarClass", parameters);

            List<Dictionary<ulong, TimeSpan>> times = new List<Dictionary<ulong, TimeSpan>>();
            foreach (var row in result)
            {
                string[] timeUnits = row["Time"].ToString().Split(':');
                TimeSpan time = new TimeSpan();
                if (timeUnits.Length == 2)
                {
                    time = new TimeSpan(0, 0, 0, int.Parse(timeUnits[0]), int.Parse(timeUnits[1]));
                }
                else if (timeUnits.Length == 3)
                {
                    time = new TimeSpan(0, 0, int.Parse(timeUnits[0]), int.Parse(timeUnits[1]), int.Parse(timeUnits[2]));
                }
                else
                {
                    continue;
                }
                Dictionary<ulong, TimeSpan> userTime = new Dictionary<ulong, TimeSpan>();
                long userIdLong = (long)row["UserID"];
                ulong userIdULon = (ulong)userIdLong;
                userTime.Add(userIdULon, time);
                times.Add(userTime);
            }
            return times;
        }
        private async Task ClearTimeCommand(SocketCommandContext context)
        {
            string channelName = context.Channel.Name;
            if (!channelName.Contains("class"))
            {
                return;
            }
            string carClass = channelName.Substring(channelName.Length - 1);
            string[] args = context.Message.Content.Split();
            if (args.Length < 2)
            {
                await context.Channel.SendMessageAsync("Invalid usage. Try !besttime TRACKNAME");
                return;
            }
            string trackName = args[1].ToLower();
            Track track = TrackService.Tracks.FirstOrDefault(trackReference => trackReference.Reference == trackName);
            if (track == null)
            {
                await context.Channel.SendMessageAsync($"Invalid track. Type !tracks to see the list of tracks");
                return;
            }
            List<SQLiteParameter> parameters = new List<SQLiteParameter>
            {
                new SQLiteParameter("@TrackName", trackName),
                new SQLiteParameter("@CarClass", carClass),
                new SQLiteParameter("@UserID", context.User.Id)
            };
            _dbManager.ExecuteQuery($"DELETE FROM RaceTimes WHERE TrackName=@TrackName AND CarClass=@CarClass AND UserID=@UserID", parameters);
            await context.Channel.SendMessageAsync($"{context.User.GlobalName} cleared his times on {track.Name} in Class {carClass.ToUpper()}");
        }
        private async Task BestTimesCommand(SocketCommandContext context)
        {
            string channelName = context.Channel.Name;
            if (!channelName.Contains("class"))
            {
                return;
            }
            string carClass = channelName.Substring(channelName.Length - 1);
            string[] args = context.Message.Content.Split();
            if (args.Length < 2)
            {
                await context.Channel.SendMessageAsync("Invalid usage. Try !besttimes TRACKNAME");
                return;
            }
            string trackName = args[1].ToLower();
            Track track = TrackService.Tracks.FirstOrDefault(trackReference => trackReference.Reference == trackName);
            if (track == null)
            {
                await context.Channel.SendMessageAsync($"Invalid track. Type !tracks to see the list of tracks");
                return;
            }

            List<SQLiteParameter> parameters = new List<SQLiteParameter>
            {
                new SQLiteParameter("@TrackName", trackName),
                new SQLiteParameter("@CarClass", carClass)
            };

            List<Dictionary<string, object>> result = _dbManager.ExecuteQuery($"SELECT * FROM RaceTimes WHERE TrackName=@TrackName AND CarClass=@CarClass", parameters);

            string leaderboard = $"__** Best times for {track.Name} Class {carClass.ToUpper()} **__ \n";

            List<Dictionary<ulong, TimeSpan>> times = GetUserTimes(trackName, carClass);
           
            var sortedList = times.OrderBy(dict => dict.Values.Min()).ToList();
            foreach (var dict in sortedList)
            {
                foreach (var pair in dict)
                {
                    string globalName = context.Guild.GetUser(pair.Key).GlobalName;
                    string formatted = pair.Value.ToString(@"mm\:ss\.fff");
                    leaderboard += $"* {formatted} - {globalName}\n";
                }
            }
            await context.Channel.SendMessageAsync(leaderboard);
        }
        private async Task ShowTracksCommand(SocketCommandContext context)
        {
            string message = "Valid tracks(Use right name for commands):\n";
            for (int i = 0; i < TrackService.Tracks.Count; i++)
            {
                message += "* " + TrackService.Tracks[i].Name + ":" + TrackService.Tracks[i].Reference + "\n";
            }
            await context.Channel.SendMessageAsync(message);
        }
        public async Task HandleCommandAsync(SocketMessage messageParam)
        {
            var message = messageParam as SocketUserMessage;
            var context = new SocketCommandContext(_client, message);
            if (message == null || message.Author.IsBot)
                return;

            int argPos = 0;
            if (message.HasCharPrefix('!', ref argPos))
            {
                var commandContext = context;
                var command = message.Content.Split()[0].Substring(1);
                if (_commandHandler.TryGetValue(command, out var handler))
                {
                    await handler(commandContext);
                }
                else
                {
                    await context.Channel.SendMessageAsync("Command not found. Type !help to see available commands.");
                }
            }
        }
        private async Task AddRaceTimeCommand(SocketCommandContext context)
        {
            string channelName = context.Channel.Name;
            if (!channelName.Contains("class"))
            {
                return;
            }
            string[] args = context.Message.Content.Split();
            if (args.Length < 3)
            {
                await context.Channel.SendMessageAsync("Invalid usage. Try !time MAP TIME");
                return;
            }
            string trackName = args[1].ToLower();
            Track track = TrackService.Tracks.FirstOrDefault(trackReferece => trackReferece.Reference == trackName);
            if (track == null)
            {
                await context.Channel.SendMessageAsync($"Invalid track. Type !tracks to see the list of tracks");
                return;
            }

            string time = args[2];
            string pattern = @"^\d{1,2}:\d{2,3}(?::\d{1,3})?$";
            Regex regex = new Regex(pattern);
            if (!regex.IsMatch(time))
            {
                await context.Channel.SendMessageAsync($"Wrong time format. Use mm:ss:mmm or ss:mmm");
                return;
            }


            string carClass = channelName.Substring(channelName.Length - 1);

            ulong userId = context.User.Id;
            SocketUser user = context.User;
            if (args.Length == 4)
            {
                if (!CheckAdmin(context.User as SocketGuildUser))
                {
                    await context.Channel.SendMessageAsync($"You don't have permission to add another player time");
                    return;
                }
                user = context.Guild.Users.FirstOrDefault(u => u.GlobalName == args[3]);
                if (user == null)
                {
                    await context.Channel.SendMessageAsync($"Invalid player");
                    return;
                }
            }

            using (var db = new RaceTimeContext())
            {
                db.RaceTimes.Add(new RaceTime { UserID = user.Id, TrackName = track.Reference, Time = time, CarClass = carClass });
                db.SaveChanges();
            }

            await context.Channel.SendMessageAsync($"Race time added for Class {carClass.ToUpper()} on {track.Name}: {time} by {user.GlobalName}");
        }
        private bool CheckAdmin(SocketGuildUser user)
        {
            var guild = user.Guild;
            return user.GuildPermissions.Administrator;
        }
    }
}
