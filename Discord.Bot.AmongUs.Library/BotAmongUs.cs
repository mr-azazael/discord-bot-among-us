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
    //Permissions.ManageEmojis
    //Permissions.ManageMessages
    //Permissions.MuteMembers
    //Permissions.DeafenMembers

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
            _CommandsExtension.RegisterCommands<TextCommands>();

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

            serviceCollection.AddSingleton(this);
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
        private Task OnGuildAvailable(DiscordClient sender, GuildCreateEventArgs arg)
        {
            return VerifyServerEmojis(arg.Guild);
        }

        /// <summary>
        /// uploads the bot emojis to the server, if possible
        /// </summary>
        /// <param name="guild">the guild on which to upload the emoji</param>
        /// <param name="channel">the channel on which to send the error messages, if allowed</param>
        /// <param name="showErrors">shows error messages</param>
        /// <returns></returns>
        public async Task VerifyServerEmojis(DiscordGuild guild, DiscordChannel channel = null, bool showErrors = false)
        {
            var missingEmojis = _Configuration.Emojis.Where(x => !guild.Emojis.Values.Any(e => e.Name == x.DiscordName));
            if (missingEmojis.Any())
            {
                //bot has the right to add emojis
                if (PermissionsManager.UserHasPermission(guild, _Client.CurrentUser, Permissions.ManageEmojis))
                {
                    //check if the server has the required amount of emoji slots
                    if (50 - guild.Emojis.Count < missingEmojis.Count())
                    {
                        if (channel != null)
                            await channel.SendMessageAsync($"the server doesn't have enough emoji slots left for me");

                        return;
                    }

                    foreach (var emoji in missingEmojis)
                    {
                        if (!guild.Emojis.Values.Any(x => x.Name == emoji.DiscordName))
                        {
                            var localFilePath = Path.Combine(Directory.GetCurrentDirectory(), emoji.LocalPath);
                            using (var stream = File.OpenRead(localFilePath))
                            {
                                await guild.CreateEmojiAsync(emoji.DiscordName, stream);
                            }
                        }
                    }
                }
                else if (showErrors)
                {
                    if(channel != null)
                        await channel.SendMessageAsync($"i dont have the right to modify emoji");
                }
            }
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
