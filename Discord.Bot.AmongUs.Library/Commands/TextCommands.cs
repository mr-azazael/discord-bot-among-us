using Discord.Bot.AmongUs.Library.Entities;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Discord.Bot.AmongUs.Library.Commands
{
    /// <summary>
    /// text commands / alternatives to the react functionality
    /// </summary>
    public class TextCommands : BaseCommandModule
    {
        [Command("stop")]
        [Description("resets an active game to the lobby status")]
        public async Task Stop(CommandContext context)
        {
            if (await CheckLobbyRights(context))
            {
                var lobbyTracker = (AmongUsLobbyTracker)context.Services.GetService(typeof(AmongUsLobbyTracker));
                var userHasLobby = lobbyTracker.UserOwnsLobby(context.User);
                if (userHasLobby)
                {
                    var lobby = lobbyTracker.GetLobbyForOwner(context.User);
                    await lobby.RestartGame(context.User);
                    await context.Message.DeleteAsync();
                }
            }
        }

        [Command("start")]
        [Description("starts a new game or resumes an ongoing game")]
        public async Task PlayGame(CommandContext context)
        {
            if (await CheckLobbyRights(context))
            {
                var lobbyTracker = (AmongUsLobbyTracker)context.Services.GetService(typeof(AmongUsLobbyTracker));
                var userHasLobby = lobbyTracker.UserOwnsLobby(context.User);
                if (userHasLobby)
                {
                    var lobby = lobbyTracker.GetLobbyForOwner(context.User);
                    if (lobby.CurrentGameStatus == GameState.InLobby || lobby.CurrentGameStatus == GameState.Paused)
                        await lobby.StartOrPauseGame(context.User);

                    await context.Message.DeleteAsync();
                }
            }
        }

        [Command("pause")]
        [Description("pauses an ongoing game")]
        public async Task PauseGame(CommandContext context)
        {
            if (await CheckLobbyRights(context))
            {
                var lobbyTracker = (AmongUsLobbyTracker)context.Services.GetService(typeof(AmongUsLobbyTracker));
                var userHasLobby = lobbyTracker.UserOwnsLobby(context.User);
                if (userHasLobby)
                {
                    var lobby = lobbyTracker.GetLobbyForOwner(context.User);
                    if (lobby.CurrentGameStatus == GameState.Running)
                        await lobby.StartOrPauseGame(context.User);

                    await context.Message.DeleteAsync();
                }
            }
        }

        [Command("create")]
        [Description("creates a lobby on the current text channel")]
        public async Task CreateLobby(CommandContext context)
        {
            if (await CheckLobbyRights(context))
            {
                await context.Message.DeleteAsync();

                var lobbyTracker = (AmongUsLobbyTracker)context.Services.GetService(typeof(AmongUsLobbyTracker));
                var userHasLobby = lobbyTracker.UserOwnsLobby(context.User);
                if (userHasLobby)
                    return;

                using (var lobby = lobbyTracker.GetLobbyForOwner(context.User))
                {
                    var lobbyInitResult = await lobby.Initialize(context);
                    if (lobbyInitResult == LobbyInitializationResult.Success)
                    {
                        await lobby.RunLobby();
                    }
                    else
                    {
                        switch (lobbyInitResult)
                        {
                            case LobbyInitializationResult.MissingEmoji:
                                {
                                    if (!PermissionsManager.UserHasPermission(context.Guild, context.Client.CurrentUser, Permissions.ManageEmojis))
                                        await context.Channel.SendMessageAsync("some required emojis aren't present on server and i dont have the right to add them");
                                    else
                                        await context.Channel.SendMessageAsync("some required emojis aren't present on server, make sure you have enough space for them");

                                    break;
                                }
                        }
                    }
                }
            }
        }

        [Command("toggle-status")]
        [Description("changes the dead/alive status of a player")]
        public async Task ToggleStatus(CommandContext context, int playerId)
        {
            await SetStatus(context, playerId, -1);
        }

        [Command("set-status")]
        [Description("sets the dead/alive status of a player")]
        public async Task SetStatus(CommandContext context, int playerId, int newStatus)
        {
            if (await CheckLobbyRights(context))
            {
                var lobbyTracker = (AmongUsLobbyTracker)context.Services.GetService(typeof(AmongUsLobbyTracker));
                var userHasLobby = lobbyTracker.UserOwnsLobby(context.User);
                if (userHasLobby)
                {
                    var lobby = lobbyTracker.GetLobbyForOwner(context.User);
                    if (lobby.CurrentGameStatus == GameState.Paused)
                        await lobby.SetPlayerStatus(context.User, playerId, newStatus);

                    await context.Message.DeleteAsync();
                }
            }
        }

        [Command("enable-voice")]
        [Description("removes the server mute/deafen from you, works only if you're in a voice channel and not in a lobby")]
        public async Task EnableVoice(CommandContext context)
        {
            if (await CheckLobbyRights(context))
            {
                var lobbyTracker = (AmongUsLobbyTracker)context.Services.GetService(typeof(AmongUsLobbyTracker));
                if (!lobbyTracker.IsUserInLobby(context.User))
                {
                    var discordMember = context.Guild.GetGuildMembers().FirstOrDefault(x => x.Id == context.User.Id);
                    if (discordMember != null)
                    {
                        if (discordMember.VoiceState?.Channel != null)
                        {
                            await discordMember.SetMuteAsync(false);
                            await discordMember.SetDeafAsync(false);
                        }
                        else
                        {
                            await context.Channel.SendMessageAsync($"{context.User.Mention}, you must be in a voice channel for this to work");
                        }
                    }
                }
            }
        }

        [Command("leave")]
        [Description("removes user from lobby")]
        public async Task LeaveLobby(CommandContext context)
        {
            var lobbyTracker = (AmongUsLobbyTracker)context.Services.GetService(typeof(AmongUsLobbyTracker));
            var lobbyForUser = lobbyTracker.GetLobbyForUser(context.User);
            if (lobbyForUser != null)
                await lobbyForUser.LeaveLobby(context.User);
        }

        [Command("rights")]
        [Description("displays the permissions required for this bot and their status")]
        [RequireUserPermissions(Permissions.SendMessages)]
        public async Task CheckRights(CommandContext context)
        {
            var builder = new DiscordEmbedBuilder
            {
                Description = "The required permissions and their status are:"
            };

            void AddPermissionField(string displayName, bool allowed)
            {
                builder.AddField(displayName, allowed ? ":white_check_mark: allowed" : ":x: denied");
            }

            AddPermissionField("Manage Emojis", PermissionsManager.UserHasPermission(context.Guild, context.Client.CurrentUser, Permissions.ManageEmojis));
            AddPermissionField("Manage Messages", PermissionsManager.UserHasPermission(context.Guild, context.Client.CurrentUser, Permissions.ManageMessages));
            AddPermissionField("Mute Members", PermissionsManager.UserHasPermission(context.Guild, context.Client.CurrentUser, Permissions.MuteMembers));
            AddPermissionField("Deafen Members", PermissionsManager.UserHasPermission(context.Guild, context.Client.CurrentUser, Permissions.DeafenMembers));

            await context.Channel.SendMessageAsync(embed: builder.Build());
        }

        [Command("emoji")]
        [Description("verifies if the required emojis are present on the server")]
        public async Task VerifyEmoji(CommandContext context)
        {
            var botAmongus = (BotAmongUs)context.Services.GetService(typeof(BotAmongUs));
            await botAmongus.VerifyServerEmojis(context.Guild, context.Channel, true);
        }

        async Task<bool> CheckLobbyRights(CommandContext context)
        {
            var canManageMessages = PermissionsManager.UserHasPermission(context.Guild, context.Client.CurrentUser, Permissions.ManageMessages);
            var canMuteMembers = PermissionsManager.UserHasPermission(context.Guild, context.Client.CurrentUser, Permissions.MuteMembers);
            var canDeafenMembers = PermissionsManager.UserHasPermission(context.Guild, context.Client.CurrentUser, Permissions.DeafenMembers);

            if (canManageMessages && canMuteMembers && canDeafenMembers)
                return true;

            var missingRights = new List<string>();
            if (!canManageMessages)
                missingRights.Add("Manage Messages");
            if (!canMuteMembers)
                missingRights.Add("Mute Members");
            if (!canDeafenMembers)
                missingRights.Add("Deafen Members");

            await context.Channel.SendMessageAsync($"I don't have rights to: {string.Join(",", missingRights)}");

            return false;
        }
    }
}