using Newtonsoft.Json;

namespace Discord.Bot.AmongUs.Library.Entities
{
    /// <summary>
    /// represents an emote on disk (to be uploaded by the bot on the server)
    /// </summary>
    public class LocalDiscordEmoji
    {
        /// <summary>
        /// the name of the emoji on discord
        /// </summary>
        [JsonProperty("name")]
        public string DiscordName { get; private set; }

        /// <summary>
        /// the local path to the emoji file
        /// </summary>
        [JsonProperty("path")]
        public string LocalPath { get; set; }

        public override string ToString()
        {
            return $"{DiscordName} from {LocalPath}";
        }
    }
}
