using Discord.Commands;
using Discord.WebSocket;
using Discord_Bot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;

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
            _commandHandler.Add("leaderboard", TrackLeaderCommand);
            _client.MessageReceived += HandleCommandAsync;
        }

        private async Task TrackLeaderCommand(SocketCommandContext context)
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

            List<Dictionary<string, object>> result = _dbManager.ExecuteQuery($"SELECT * FROM RaceTimes WHERE TrackName='{trackName}' AND CarClass='{carClass}'");

            string leaderboard = $"__** Leaderboard for {track.Name} Class {carClass.ToUpper()} **__ \n";

            List<Dictionary<string, TimeSpan>> times = new List<Dictionary<string, TimeSpan>>();
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
                    await Console.Out.WriteLineAsync("Invalid time format");
                    continue;
                }
                Dictionary<string, TimeSpan> userTime = new Dictionary<string, TimeSpan>();
                userTime.Add(row["User"].ToString(), time);
                times.Add(userTime);
            }
            var sortedList = times.OrderBy(dict => dict.Values.Min()).ToList();
            foreach (var dict in sortedList)
            {
                foreach (var pair in dict)
                {
                    string formatted = pair.Value.ToString(@"mm\:ss\.fff");
                    leaderboard += $"* {formatted} - {pair.Key}\n";
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
            string channelName = context.Channel.Name;
            if (!channelName.Contains("class"))
            {
                return;
            }
            string time = args[2];
            string carClass = channelName.Substring(channelName.Length - 1);
            using (var db = new RaceTimeContext())
            {
                db.RaceTimes.Add(new RaceTime { User = context.User.Username, TrackName = track.Reference, Time = time, CarClass = carClass });
                db.SaveChanges();
            }

            await context.Channel.SendMessageAsync($"Race time added for Class {carClass.ToUpper()} on {track.Name}: {time} by {context.User.Username}");
        }
    }
}
