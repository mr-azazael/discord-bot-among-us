using DSharpPlus.Entities;
using Newtonsoft.Json;

namespace Discord.Bot.AmongUs.Library.Entities
{
    /// <summary>
    /// represents the local mapping between the emoji and the among us player (and its states - dead or alive)
    /// </summary>
    public class AmongUsPayerEmoji
    {
        /// <summary>
        /// the emoji name for the alive state
        /// </summary>
        [JsonProperty("alive")]
        public string LocalEmojiAliveName { get; private set; }

        /// <summary>
        /// formatted emoji name for <see cref="LocalEmojiAliveName"/>
        /// </summary>
        public string DiscordEmojiAlive
        {
            get => $":{LocalEmojiAliveName}:";
        }

        /// <summary>
        /// the emoji name for the dead state
        /// </summary>
        [JsonProperty("dead")]
        public string LocalEmojiDeadName { get; private set; }

        /// <summary>
        /// formatted emoji name for <see cref="LocalEmojiDeadName"/>
        /// </summary>
        public string DiscordEmojiDead
        {
            get => $":{LocalEmojiDeadName}:";
        }

        /// <summary>
        /// general color of the emoji
        /// </summary>
        [JsonProperty("color")]
        public string Color { get; private set; }

        /// <summary>
        /// returns the color represented by<see cref="Color"/>
        /// </summary>
        public DiscordColor EmojiColor
        {
            get => new DiscordColor(Color);
        }

        public override string ToString()
        {
            return $"{DiscordEmojiAlive} {EmojiColor}";
        }
    }
}
