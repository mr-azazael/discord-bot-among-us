using DSharpPlus.Entities;

namespace Discord.Bot.AmongUs.Library.Entities
{
    /// <summary>
    /// represents an among us player in the lobby
    /// </summary>
    class AmongUsPlayer
    {
        public AmongUsPlayer(AmongUsPayerEmoji emoji, int id)
        {
            Id = id;
            Emoji = emoji;
        }

        /// <summary>
        /// an unique id for this player in this lobby
        /// </summary>
        public int Id { get; }

        /// <summary>
        /// data container for the embed field
        /// </summary>
        public AmongUsPlayerEmbedData CurrentData { get; private set; } = new AmongUsPlayerEmbedData();

        /// <summary>
        /// data container for the emoji representing this player
        /// </summary>
        public AmongUsPayerEmoji Emoji { get; private set; }

        /// <summary>
        /// discord emoji for alive state
        /// </summary>
        public DiscordEmoji Alive { get; set; }

        /// <summary>
        /// discord emoji for the dead state
        /// </summary>
        public DiscordEmoji Dead { get; set; }

        /// <summary>
        /// the discord used assigned to this player (joine this slot)
        /// </summary>
        public DiscordUser AssignedUser { get; set; }

        /// <summary>
        /// the embed field generated for this user
        /// </summary>
        public DiscordEmbedField AssignedField { get; set; }

        /// <summary>
        /// dead or alive state
        /// </summary>
        public bool IsAlive { get; set; } = true;

        public override string ToString()
        {
            return $"{AssignedUser?.Username ?? "empty"} - {Emoji.DiscordEmojiAlive}";
        }
    }
}
