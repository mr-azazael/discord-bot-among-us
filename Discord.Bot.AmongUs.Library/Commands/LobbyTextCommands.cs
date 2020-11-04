using Discord.Bot.AmongUs.Library.Entities;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using System.Linq;
using System.Threading.Tasks;

namespace Discord.Bot.AmongUs.Library.Commands
{
    /// <summary>
    /// text commands / alternatives to the react functionality
    /// </summary>
    public class LobbyTextCommands : BaseCommandModule
    {
        [Command("stop")]
        [Description("resets an active game to the lobby status")]
        public async Task Stop(CommandContext context)
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

        [Command("start")]
        [Description("starts a new game or resumes an ongoing game")]
        public async Task PlayGame(CommandContext context)
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

        [Command("pause")]
        [Description("pauses an ongoing game")]
        public async Task PauseGame(CommandContext context)
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

        [Command("create")]
        [Description("creates a lobby on the current text channel")]
        public async Task CreateLobby(CommandContext context)
        {
            await context.Message.DeleteAsync();
            
            var lobbyTracker = (AmongUsLobbyTracker)context.Services.GetService(typeof(AmongUsLobbyTracker));
            var userHasLobby = lobbyTracker.UserOwnsLobby(context.User);
            if (userHasLobby)
                return;

            using (var lobby = lobbyTracker.GetLobbyForOwner(context.User))
            {
                await lobby.Initialize(context);
                await lobby.RunLobby();
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
            var lobbyTracker = (AmongUsLobbyTracker)context.Services.GetService(typeof(AmongUsLobbyTracker));
            var userHasLobby = lobbyTracker.UserOwnsLobby(context.User);
            if (userHasLobby)
            {
                var lobby = lobbyTracker.GetLobbyForOwner(context.User);
                if (lobby.CurrentGameStatus == GameState.Paused)
                    await lobby.SetPlayerStatus(context.User, playerId);

                await context.Message.DeleteAsync();
            }
        }

        [Command("enable-voice")]
        [Description("removes the server mute/deafen from you, works only if you're in a voice channel and not in a lobby")]
        public async Task EnableVoice(CommandContext context)
        {
            var lobbyTracker = (AmongUsLobbyTracker)context.Services.GetService(typeof(AmongUsLobbyTracker));
            if(!lobbyTracker.IsUserInLobby(context.User))
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

        [Command("leave")]
        [Description("removes user from lobby")]
        public async Task LeaveLobby(CommandContext context)
        {
            var lobbyTracker = (AmongUsLobbyTracker)context.Services.GetService(typeof(AmongUsLobbyTracker));
            var lobbyForUser = lobbyTracker.GetLobbyForUser(context.User);
            if (lobbyForUser != null)
                await lobbyForUser.LeaveLobby(context.User);
        }
    }
}
