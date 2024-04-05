using Discord;
using Discord.WebSocket;
using Discord.Commands;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace DiscordBot
{
    class Bot
    {
        private DiscordSocketClient _client;
        private CommandService _commands;
        private IServiceProvider _services;
        private CommandHandler _commandHandler;

        private Task LogAsync(LogMessage log)
        {
            Console.WriteLine(log.ToString());
            return Task.CompletedTask;
        }
        public Bot()
        {

        }
        public async Task MainAsync()
        {
            var config = new DiscordSocketConfig
            {
                GatewayIntents = GatewayIntents.All | GatewayIntents.MessageContent
            };
            _client = new DiscordSocketClient(config);
            _commands = new CommandService();
            _services = new ServiceCollection()
                 .AddSingleton(_client)
                 .AddSingleton(_commands)
                 .BuildServiceProvider();

            _client.Log += LogAsync;

            _commandHandler = new CommandHandler(_client, _commands);


            await _client.LoginAsync(TokenType.Bot, "");
            await _client.StartAsync();

            await Task.Delay(-1);
        }
    }
}