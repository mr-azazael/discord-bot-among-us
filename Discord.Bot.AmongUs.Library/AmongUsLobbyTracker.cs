using DSharpPlus.Entities;
using System.Collections.Generic;
using System.Linq;

namespace Discord.Bot.AmongUs.Library
{
    /// <summary>
    /// tracks the lobbies created by this bot
    /// </summary>
    class AmongUsLobbyTracker
    {
        Dictionary<ulong, AmongUsLobby> _ActiveLobbies = new Dictionary<ulong, AmongUsLobby>();
        JsonConfiguration _Configuration;

        public AmongUsLobbyTracker(JsonConfiguration configuration)
        {
            _Configuration = configuration;
        }

        /// <summary>
        /// returns true if the user has joined any lobby
        /// </summary>
        /// <param name="user">the user to check against</param>
        /// <returns>true if the user is in a lobby</returns>
        public bool IsUserInLobby(DiscordUser user)
        {
            return _ActiveLobbies.Values.Any(x => x.IsUserInLobby(user));
        }

        /// <summary>
        /// returns the lobby which contains this user
        /// </summary>
        /// <param name="user">the user for which the lobby will be searched</param>
        /// <returns>the lobby which contains the given user</returns>
        public AmongUsLobby GetLobbyForUser(DiscordUser user)
        {
            return _ActiveLobbies.Values.FirstOrDefault(x => x.IsUserInLobby(user));
        }

        /// <summary>
        /// returns true if the given user owns a lobby
        /// </summary>
        /// <param name="user">the user to check against</param>
        /// <returns>true if the user owns a lobby</returns>
        public bool UserOwnsLobby(DiscordUser user)
        {
            return _ActiveLobbies.ContainsKey(user.Id);
        }

        /// <summary>
        /// returns the owned lobby by the given user
        /// </summary>
        /// <param name="user">the user for which to search the lobby</param>
        /// <returns></returns>
        public AmongUsLobby GetLobbyForOwner(DiscordUser user)
        {
            if (!_ActiveLobbies.ContainsKey(user.Id))
                _ActiveLobbies[user.Id] = new AmongUsLobby(this, _Configuration);

            return _ActiveLobbies[user.Id];
        }

        /// <summary>
        /// removes the lobby from tracking for the given usser
        /// </summary>
        /// <param name="user">the user for which to remove the associated lobby</param>
        public void RemoveOwnedLobby(DiscordUser user)
        {
            if (_ActiveLobbies.ContainsKey(user.Id))
                _ActiveLobbies.Remove(user.Id);
        }
    }
}
