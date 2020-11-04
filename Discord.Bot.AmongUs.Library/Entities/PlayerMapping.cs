using DSharpPlus.Entities;

namespace Discord.Bot.AmongUs.Library.Entities
{
    /// <summary>
    /// represents a mapping between a discord member and an among us <br/>
    /// player in lobby for controlling the voice
    /// </summary>
    class PlayerMapping
    {
        public AmongUsPlayer Player { get; set; }
        public DiscordMember Member { get; set; }
    }
}
