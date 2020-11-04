using Discord.Bot.AmongUs.Library.Commands;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Discord.Bot.AmongUs.Library
{
    //PERMISSIONS INTEGER 1086400576

    /// <summary>
    /// the bot implementation
    /// </summary>
    public class BotAmongUs
    {
        DiscordClient _Client;
        CommandsNextExtension _CommandsExtension;
        JsonConfiguration _Configuration;

        public BotAmongUs(JsonConfiguration configuration)
        {
            _Configuration = configuration;
            var config = new DiscordConfiguration
            {
                Token = _Configuration.Token,
                TokenType = TokenType.Bot,
                AutoReconnect = true,
                MinimumLogLevel = LogLevel.Debug
            };
            _Client = new DiscordClient(config);

            var commands = new CommandsNextConfiguration
            {
                StringPrefixes = new List<string> { _Configuration.Prefix },
                EnableDms = false,
                Services = GetServiceProvider()
            };
            _CommandsExtension = _Client.UseCommandsNext(commands);
            _CommandsExtension.RegisterCommands<LobbyTextCommands>();

            var interactivityConfiguration = new InteractivityConfiguration
            {
                Timeout = TimeSpan.FromMinutes(5)
            };
            
            _Client.UseInteractivity(interactivityConfiguration);
            _Client.Ready += OnClientIsReady;
            _Client.GuildAvailable += OnGuildAvailable;
        }

        /// <summary>
        /// constructs the service provider
        /// </summary>
        /// <returns>the service provider</returns>
        ServiceProvider GetServiceProvider()
        {
            var serviceCollection = new ServiceCollection();

            serviceCollection.AddSingleton(new AmongUsLobbyTracker(_Configuration));

            return serviceCollection.BuildServiceProvider();
        }

        /// <summary>
        /// event fired when the discord client is ready <br/>
        /// sets the bot status
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <returns></returns>
        private async Task OnClientIsReady(DiscordClient sender, ReadyEventArgs e)
        {
            await sender.UpdateStatusAsync(new DiscordActivity
            {
                ActivityType = ActivityType.ListeningTo,
                Name = "bau@help"
            }, UserStatus.Online);
        }

        /// <summary>
        /// event fired when a guild becomes available <br/>
        /// uploads the available emojis to the guild
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <returns></returns>
        private Task OnGuildAvailable(DiscordClient sender, GuildCreateEventArgs e)
        {
            Task.Run(async () =>
            {
                foreach (var emoji in _Configuration.Emojis)
                {
                    if (!e.Guild.Emojis.Values.Any(x => x.Name == emoji.DiscordName))
                    {
                        var localFilePath = Path.Combine(Directory.GetCurrentDirectory(), emoji.LocalPath);
                        using (var stream = File.OpenRead(localFilePath))
                        {
                            await e.Guild.CreateEmojiAsync(emoji.DiscordName, stream);
                        }
                    }
                }
            });

            return Task.CompletedTask;
        }

        /// <summary>
        /// starts the discord connection
        /// </summary>
        /// <returns></returns>
        public async Task ConnectAsync()
        {
            await _Client.ConnectAsync();
        }

        /// <summary>
        /// stops the discord connection
        /// </summary>
        /// <returns></returns>
        public async Task DisconnectAsync()
        {
            await _Client.DisconnectAsync();
        }
    }
}
