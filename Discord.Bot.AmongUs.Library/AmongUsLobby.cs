using Discord.Bot.AmongUs.Library.Entities;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Discord.Bot.AmongUs.Library
{
    /// <summary>
    /// represents an among us game lobby
    /// </summary>
    class AmongUsLobby : IDisposable
    {
        public CommandContext Context { get; private set; }
        public GameState? CurrentGameStatus { get; private set; }

        AmongUsLobbyTracker _Tracker;
        DiscordEmbedBuilder _EmbedBuilder;
        DiscordMessage _EmbedMessage;
        InteractivityExtension _Interactivity;
        JsonConfiguration _Configuration;

        DiscordMessage _OwnerLeftTheLobbyMessage;
        List<AmongUsPlayer> _Players;
        List<DiscordEmoji> _ValidReactEmojis;
        List<DiscordMessage> _ToBeDeletedMessages;

        DiscordEmoji _LeaveLobby;
        DiscordEmoji _PlayGame;
        DiscordEmoji _StopGame;

        public AmongUsLobby(AmongUsLobbyTracker tracker, JsonConfiguration configuration)
        {
            var playerId = 1;
            _Tracker = tracker;
            _Configuration = configuration;
            _EmbedBuilder = new DiscordEmbedBuilder { Color = DiscordColor.Red };
            _Players = configuration.AmongUsEmoji.Select(x => new AmongUsPlayer(x, playerId++)).ToList();
            _ValidReactEmojis = new List<DiscordEmoji>();
            _ToBeDeletedMessages = new List<DiscordMessage>();
        }

        /// <summary>
        /// initializes the private data for the lobby
        /// </summary>
        /// <param name="context">the command context from which the lobby will be initialized</param>
        /// <returns></returns>
        public async Task<LobbyInitializationResult> Initialize(CommandContext context)
        {
            Context = context;

            _EmbedBuilder.Author = new DiscordEmbedBuilder.EmbedAuthor
            {
                IconUrl = Context.User.AvatarUrl
            };

            foreach (var player in _Players)
            {
                if (!CheckIfEmojiExistsOnServer(player.Emoji.DiscordEmojiAlive) || !CheckIfEmojiExistsOnServer(player.Emoji.DiscordEmojiDead))
                    return LobbyInitializationResult.MissingEmoji;

                player.Alive = DiscordEmoji.FromName(Context.Client, player.Emoji.DiscordEmojiAlive);
                player.Dead = DiscordEmoji.FromName(Context.Client, player.Emoji.DiscordEmojiDead);
                _ValidReactEmojis.Add(player.Alive);
            }

            CurrentGameStatus = await UpdateGameState(GameState.StartUp, null);
            _EmbedMessage = await Context.Channel.SendMessageAsync(embed: _EmbedBuilder);
            _ToBeDeletedMessages.Add(_EmbedMessage);

            if (!CheckIfEmojiExistsOnServer(_Configuration.LeaveLobbyEmojiName))
                return LobbyInitializationResult.MissingEmoji;

            if (!CheckIfEmojiExistsOnServer(_Configuration.PlayPauseEmojiName))
                return LobbyInitializationResult.MissingEmoji;

            if (!CheckIfEmojiExistsOnServer(_Configuration.EndGameEmojiName))
                return LobbyInitializationResult.MissingEmoji;

            _LeaveLobby = DiscordEmoji.FromName(Context.Client, _Configuration.LeaveLobbyEmojiName);
            _PlayGame = DiscordEmoji.FromName(Context.Client, _Configuration.PlayPauseEmojiName);
            _StopGame = DiscordEmoji.FromName(Context.Client, _Configuration.EndGameEmojiName);
            _ValidReactEmojis.AddRange(_LeaveLobby, _PlayGame, _StopGame);

            await _EmbedMessage.CreateReactionAsync(_PlayGame);
            await _EmbedMessage.CreateReactionAsync(_StopGame);

            foreach (var player in _Players)
            {
                await _EmbedMessage.CreateReactionAsync(player.Alive);
                await Task.Delay(250);
            }

            await _EmbedMessage.CreateReactionAsync(_LeaveLobby);

            _Interactivity = Context.Client.GetInteractivity();
            CurrentGameStatus = await UpdateGameState(GameState.InLobby, null);

            return LobbyInitializationResult.Success;
        }

        /// <summary>
        /// listens for reactions from users and responds accordingly
        /// </summary>
        /// <returns></returns>
        public async Task<GameState> RunLobby()
        {
            var timeOut = TimeSpan.FromSeconds(1);
            while (CurrentGameStatus != GameState.Finished)
            {
                var emojiReaction = await _Interactivity.WaitForReactionAsync(x => x.Message == _EmbedMessage, timeOut);
                if (!emojiReaction.TimedOut)
                {
                    var emoji = emojiReaction.Result.Emoji;
                    var user = emojiReaction.Result.User;

                    if (_ValidReactEmojis.Contains(emoji))
                    {
                        if (emoji == _PlayGame)
                        {
                            await StartOrPauseGame(user);
                        }
                        else if (emoji == _StopGame)
                        {
                            await RestartGame(user);
                        }
                        else if (emoji == _LeaveLobby)
                        {
                            if (await LeaveLobby(user))
                                break;
                        }
                        else if (emoji != _LeaveLobby)
                        {
                            await OnEmbedMessageReaction(user, emoji);
                        }
                    }
                    else
                    {
                        //delete unwanted reactions
                        await _EmbedMessage.DeleteReactionsEmojiAsync(emoji);
                    }

                    //delete the latest reaction
                    await _EmbedMessage.DeleteReactionAsync(emoji, user);
                }
            }

            return CurrentGameStatus.GetValueOrDefault();
        }

        /// <summary>
        /// handles the start-pause-resume logic
        /// </summary>
        /// <param name="user">the user who requested the action</param>
        /// <returns></returns>
        public async Task StartOrPauseGame(DiscordUser user)
        {
            //only the lobby master can do this operation
            if (user == Context.User)
            {
                //start if we're in loby, resume if we're in pause
                if (CurrentGameStatus == GameState.InLobby || CurrentGameStatus == GameState.Paused)
                {
                    if (CurrentGameStatus == GameState.InLobby)
                        foreach (var player in _Players)
                            player.IsAlive = true;

                    UpdateEmbededFields(false);
                    var playerMapping = GetPlayerToDiscordUserMapping(_Players);
                    CurrentGameStatus = await UpdateGameState(GameState.Running);
                }
                else if (CurrentGameStatus == GameState.Running)
                {
                    UpdateEmbededFields(false);
                    var playerMapping = GetPlayerToDiscordUserMapping(_Players);
                    CurrentGameStatus = await UpdateGameState(GameState.Paused);
                }
            }
        }

        /// <summary>
        /// restarts a game (sets players to alive, unmutes the voice channel)
        /// </summary>
        /// <param name="user">the user who requested the action</param>
        /// <returns></returns>
        public async Task RestartGame(DiscordUser user)
        {
            //only the lobby master can do this operation
            if (user == Context.User)
            {
                if (CurrentGameStatus == GameState.Running || CurrentGameStatus == GameState.Paused)
                {
                    foreach (var player in _Players)
                        player.IsAlive = true;

                    UpdateEmbededFields(false);
                    var playerMapping = GetPlayerToDiscordUserMapping(_Players);
                    CurrentGameStatus = await UpdateGameState(GameState.InLobby);
                }
            }
        }

        /// <summary>
        /// removes an user from the lobby, removes the lobby when it's the case
        /// </summary>
        /// <param name="user">the user who requested the action</param>
        /// <returns></returns>
        public async Task<bool> LeaveLobby(DiscordUser user)
        {
            //the lobby master left the lobby, close the lobby
            if (user == Context.User)//leave only when not running
            {
                var playerMapping = GetPlayerToDiscordUserMapping(_Players);
                _OwnerLeftTheLobbyMessage = await Context.Channel.SendMessageAsync($"{Context.User.Username} has left the building, the lobby was closed");
                CurrentGameStatus = await UpdateGameState(GameState.Finished);
                _Tracker.RemoveOwnedLobby(Context.User);

                return true;
            }

            //someone left the lobby
            var player = _Players.FirstOrDefault(x => x.AssignedUser == user);
            if (player != null)
            {
                player.AssignedUser = null;
                UpdateEmbededFields();
            }

            return false;
        }

        /// <summary>
        /// removes sent messages from the discord channel
        /// </summary>
        /// <returns></returns>
        async Task CleanupMessages()
        {
            //clear the clutter
            var deleteMessagesTasks = new List<Task>();
            foreach (var trashMessage in _ToBeDeletedMessages)
                deleteMessagesTasks.Add(trashMessage.DeleteAsync());

            _ToBeDeletedMessages.Clear();
            Task.WaitAll(deleteMessagesTasks.ToArray());

            if (_OwnerLeftTheLobbyMessage != null)
            {
                var ownerLeftMessage = _OwnerLeftTheLobbyMessage;
                _OwnerLeftTheLobbyMessage = null;
                await Task.Delay(TimeSpan.FromMinutes(1));
                await ownerLeftMessage.DeleteAsync();
            }
        }

        /// <summary>
        /// checks if the given user is in this lobby
        /// </summary>
        /// <param name="user">the user to look for</param>
        /// <returns>true if the user is in this lobby, false otherwise</returns>
        public bool IsUserInLobby(DiscordUser user)
        {
            return _Players.Any(x => x.AssignedUser == user);
        }

        /// <summary>
        /// handles a reaction 
        /// </summary>
        /// <param name="user">the user who reacted to the embeded message</param>
        /// <param name="emoji">the emoji to which the user reacted</param>
        /// <returns></returns>
        async Task OnEmbedMessageReaction(DiscordUser user, DiscordEmoji emoji)
        {
            if (CurrentGameStatus == GameState.InLobby)
            {
                var currentLobby = _Tracker.GetLobbyForUser(user);
                if (currentLobby != null && currentLobby != this)
                {
                    await Context.Channel.SendMessageAsync($"{user.Mention}, you've already joined a lobby");
                    return;
                }

                if (_Players.Count(x => x.AssignedUser != null) == _Configuration.MaxPlayerCount)
                {
                    var message = await Context.Channel.SendMessageAsync($"max player limit reached for {Context.User.Username}'s lobby");
                    _ToBeDeletedMessages.Add(message);
                    return;
                }

                //someone tries to join a slot
                var currentSlot = _Players.FirstOrDefault(x => x.AssignedUser == user);
                var targetSlot = _Players.FirstOrDefault(x => x.Alive == emoji);

                if (targetSlot.AssignedUser == null)
                {
                    if (currentSlot != null)
                        currentSlot.AssignedUser = null;

                    var updateEmbedColor = user == Context.User;
                    targetSlot.AssignedUser = user;
                    UpdateAuthorDescription();
                    UpdateEmbededFields(!updateEmbedColor);

                    if (updateEmbedColor)
                        await UpdateGameState(GameState.InLobby, targetSlot.Emoji.EmojiColor);
                }
            }
            else if (CurrentGameStatus == GameState.Paused)
            {
                //player tries to set dead/alive state                
                var player = _Players.First(x => x.Alive == emoji);
                await SetPlayerStatus(user, player.Id);
            }
        }

        /// <summary>
        /// sets the player status
        /// </summary>
        /// <param name="user">the user who requested the change</param>
        /// <param name="playerId">the id of the player to be updated</param>
        /// <param name="newStatus">new player status: -1 for next, 0 for dead, 1 for alive</param>
        /// <returns></returns>
        public async Task SetPlayerStatus(DiscordUser user, int playerId, int newStatus = -1)
        {
            //only loby master can do this - allow self kill? || reactedEmoji.Result.User == player.AssignedUser
            if (user == Context.User)
            {
                var player = _Players.FirstOrDefault(x => x.Id == playerId);
                if (player?.AssignedUser != null)
                {
                    if (newStatus == -1)
                        player.IsAlive = !player.IsAlive;
                    else if (newStatus == 0)
                        player.IsAlive = false;
                    else if (newStatus == 1)
                        player.IsAlive = true;

                    UpdateEmbededFields();

                    //mute dead players when status is set
                    var discordMember = Context.Guild.GetGuildMembers().FirstOrDefault(x => x.Id == player.AssignedUser.Id);
                    if (discordMember != null && discordMember.VoiceState?.Channel != null)
                        await discordMember.SetMuteAsync(!player.IsAlive);
                }
            }
        }

        /// <summary>
        /// maps the lobby players to discord member values
        /// </summary>
        /// <param name="players">the players tom map</param>
        /// <returns></returns>
        List<PlayerMapping> GetPlayerToDiscordUserMapping(List<AmongUsPlayer> players)
        {
            var mapping = new List<PlayerMapping>();
            var guildMembers = Context.Guild.GetGuildMembers();

            foreach (var player in players)
            {
                if (player.AssignedUser != null)
                {
                    var discordMember = guildMembers.FirstOrDefault(x => x.Id == player.AssignedUser.Id);
                    if (discordMember != null)
                        mapping.Add(new PlayerMapping { Player = player, Member = discordMember });
                }
            }

            return mapping;
        }

        /// <summary>
        /// updates the embeded message and sets mute/deafen accordingly
        /// </summary>
        /// <param name="newGameState">the new game state to apply</param>
        /// <param name="voiceChannelMembers">members in the lobby</param>
        /// <param name="ownerColor">the owner's color for cosmetic purposes only</param>
        /// <returns></returns>
        async Task<GameState> UpdateGameState(GameState newGameState, DiscordColor? ownerColor = null)
        {
            switch (newGameState)
            {
                case GameState.StartUp:
                    {
                        _EmbedBuilder.Title = "loading, please wait";
                        _EmbedBuilder.Description = null;
                        _EmbedBuilder.Footer = null;
                        UpdateAuthorDescription();

                        break;
                    }
                case GameState.InLobby:
                    {
                        _EmbedBuilder.Title = "waiting for players";
                        _EmbedBuilder.Description = "click on an emoji to join or x to leave this lobby";
                        _EmbedBuilder.Footer = null;
                        UpdateAuthorDescription();

                        var voiceChannelMembers = GetPlayerToDiscordUserMapping(_Players);
                        if (voiceChannelMembers != null)
                        {
                            foreach (var mapping in voiceChannelMembers)
                            {
                                if (mapping.Member.VoiceState?.Channel != null)
                                {
                                    await mapping.Member.SetMuteAsync(false);
                                    await mapping.Member.SetDeafAsync(false);
                                }
                            }
                        }

                        break;
                    }
                case GameState.Running:
                    {
                        _EmbedBuilder.Title = "game started";
                        _EmbedBuilder.Description = "do your tasks / kill the crewmates";
                        _EmbedBuilder.Footer = new DiscordEmbedBuilder.EmbedFooter
                        {
                            Text = $"{Context.User.Username} can use play/pause and stop to control the game state"
                        };
                        UpdateAuthorDescription();

                        var voiceChannelMembers = GetPlayerToDiscordUserMapping(_Players);
                        if (voiceChannelMembers != null)
                        {
                            //mute and deafen alive players, unmute dead players
                            foreach (var mapping in voiceChannelMembers.OrderBy(x => x.Player.IsAlive))
                            {
                                if (mapping.Member.VoiceState?.Channel != null)
                                {
                                    await mapping.Member.SetMuteAsync(mapping.Player.IsAlive);
                                    await mapping.Member.SetDeafAsync(mapping.Player.IsAlive);
                                }
                            }
                        }

                        break;
                    }
                case GameState.Paused:
                    {
                        _EmbedBuilder.Title = "game paused";
                        _EmbedBuilder.Description = "discuss/vote";
                        _EmbedBuilder.Footer = new DiscordEmbedBuilder.EmbedFooter
                        {
                            Text = $"{Context.User.Username} can click the player emoji to set dead or alive state"
                        };
                        UpdateAuthorDescription();

                        var voiceChannelMembers = GetPlayerToDiscordUserMapping(_Players);
                        if (voiceChannelMembers != null)
                        {
                            //unmute and undeafen alive players, mute dead players
                            foreach (var mapping in voiceChannelMembers.OrderBy(x => x.Player.IsAlive))
                            {
                                if (mapping.Member.VoiceState?.Channel != null)
                                {
                                    await mapping.Member.SetMuteAsync(!mapping.Player.IsAlive);
                                    if (mapping.Player.IsAlive)
                                        await mapping.Member.SetDeafAsync(!mapping.Player.IsAlive);
                                }
                            }
                        }

                        break;
                    }
                case GameState.Finished:
                    {
                        _EmbedBuilder.Title = "closing lobby";
                        _EmbedBuilder.Description = null;
                        _EmbedBuilder.Footer = null;
                        UpdateAuthorDescription();

                        var voiceChannelMembers = GetPlayerToDiscordUserMapping(_Players);
                        if (voiceChannelMembers != null)
                        {
                            foreach (var mapping in voiceChannelMembers)
                            {
                                if (mapping.Member.VoiceState?.Channel != null)
                                {
                                    await mapping.Member.SetMuteAsync(false);
                                    await mapping.Member.SetDeafAsync(false);
                                }
                            }
                        }

                        break;
                    }
            }

            if (ownerColor != null)
                _EmbedBuilder.Color = ownerColor.Value;

            if (_EmbedMessage != null)
                await _EmbedMessage.ModifyAsync(embed: _EmbedBuilder.Build());

            return newGameState;
        }

        /// <summary>
        /// verifies if the current player data corresponds with what's was displayed in the last update
        /// </summary>
        /// <param name="players">the list with all the players</param>
        /// <param name="updateMessage">should update the embeded message in this call?</param>
        async void UpdateEmbededFields(bool updateMessage = true)
        {
            bool shouldUpdateTheMessage = false;
            foreach (var player in _Players)
            {
                var computedEmbedData = AmongUsPlayerEmbedData.GeneratePlayerData(player);
                if (player.CurrentData.DataIsObsolete(computedEmbedData))
                {
                    //remove old field
                    var fieldIndex = _EmbedBuilder.Fields.IndexOf(player.AssignedField);
                    if (fieldIndex > -1)
                    {
                        _EmbedBuilder.RemoveFieldAt(fieldIndex);
                        shouldUpdateTheMessage = true;
                    }

                    player.CurrentData.UpdateData(computedEmbedData);

                    if (player.CurrentData.ShouldDisplayEmbedData())
                    {
                        _EmbedBuilder.AddField(player.CurrentData.Name, player.CurrentData.Value, true);
                        player.AssignedField = _EmbedBuilder.Fields.Last();
                        shouldUpdateTheMessage = true;
                    }
                }
            }

            //change the displayed info
            if (updateMessage && shouldUpdateTheMessage)
                await _EmbedMessage.ModifyAsync(embed: _EmbedBuilder.Build());
        }

        /// <summary>
        /// checks if the given emoji exists on server
        /// </summary>
        /// <returns>true if the emoji exists on server</returns>
        bool CheckIfEmojiExistsOnServer(params string[] emojiName)
        {
            return emojiName.All(e => Context.Guild.Emojis.Values.Any(x => x.Name == e));
        }

        /// <summary>
        /// updates the author description
        /// </summary>
        void UpdateAuthorDescription()
        {
            var authorName = $"{Context.User.Username}'s lobby";
            authorName = authorName.PadRight(_Configuration.EmbedAuthorMaxLength);
            int playerCount = _Players.Count(x => x.AssignedUser != null);
            _EmbedBuilder.Author.Name = $"{authorName} {playerCount} / {_Configuration.MaxPlayerCount}";
        }

        /// <summary>
        /// IDispose implementation
        /// </summary>
        public async void Dispose()
        {
            await CleanupMessages();
        }
    }
}
