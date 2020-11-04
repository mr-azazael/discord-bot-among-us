using Discord.Bot.AmongUs.Library.Entities;
using Newtonsoft.Json;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Discord.Bot.AmongUs.Library
{
    public class JsonConfiguration
    {
        public int MaxPlayerCount { get; private set; } = 10;

        /// <summary>
        /// bot token
        /// </summary>
        [JsonProperty("token")]
        public string Token { get; private set; }

        /// <summary>
        /// bot prefix
        /// </summary>
        [JsonProperty("prefix")]
        public string Prefix { get; private set; }

        /// <summary>
        /// name of the emoji for leaving the lobby
        /// </summary>
        [JsonProperty("leave-lobby")]
        public string LocalLeaveLobbyEmojiName { get; private set; }

        /// <summary>
        /// formatted emoji name for <see cref="LocalLeaveLobbyEmojiName"/>
        /// </summary>
        public string LeaveLobbyEmojiName
        {
            get => $":{LocalLeaveLobbyEmojiName}:";
        }

        /// <summary>
        /// name of the emoji for controllig the game
        /// </summary>
        [JsonProperty("play-pause")]
        public string LocalPlayPauseEmojiName { get; private set; }

        /// <summary>
        /// formatted emoji name for <see cref="LocalPlayPauseEmojiName"/>
        /// </summary>
        public string PlayPauseEmojiName
        {
            get => $":{LocalPlayPauseEmojiName}:";
        }

        /// <summary>
        /// name of the emoji for reseting the game
        /// </summary>
        [JsonProperty("end-game")]
        public string LocalEndGameEmojiName { get; private set; }

        /// <summary>
        /// formatted emoji name for <see cref="EndGameEmojiName"/>
        /// </summary>
        public string EndGameEmojiName
        {
            get => $":{LocalEndGameEmojiName}:";
        }

        /// <summary>
        /// the emoji list this bot will upload to the server
        /// </summary>
        [JsonProperty("emojis")]
        public LocalDiscordEmoji[] Emojis { get; private set; }

        /// <summary>
        /// emoji mapping for players
        /// </summary>
        [JsonProperty("amongus-emojis")]
        public AmongUsPayerEmoji[] AmongUsEmoji { get; private set; }

        /// <summary>
        /// loads the configuration from the disk
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public static async Task<JsonConfiguration> LoadConfiguration(string fileName)
        {
            using (var fileStream = File.OpenRead(fileName))
            {
                using (var streamReader = new StreamReader(fileStream, new UTF8Encoding(false)))
                {
                    var stringContent = await streamReader.ReadToEndAsync();
                    return JsonConvert.DeserializeObject<JsonConfiguration>(stringContent);
                }
            }
        }
    }
}
