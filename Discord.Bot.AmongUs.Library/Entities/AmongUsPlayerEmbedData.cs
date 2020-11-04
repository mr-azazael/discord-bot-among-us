namespace Discord.Bot.AmongUs.Library.Entities
{
    /// <summary>
    /// represents the embed field for a player
    /// </summary>
    class AmongUsPlayerEmbedData
    {
        /// <summary>
        /// contains the value for the name part of the embed field
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// contains the value for the value part of the embed field
        /// </summary>
        public string Value { get; set; }

        /// <summary>
        /// generates the name/value for a given player
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
        public static (string name, string value) GeneratePlayerData(AmongUsPlayer player)
        {
            string name = null;
            string value = null;

            if (player.AssignedUser != null)
            {
                var emoji = player.IsAlive ? player.Alive : player.Dead;
                var status = player.IsAlive ? "alive" : "dead";

                name = $"{player.Id}: {emoji} `{status}`";
                value = $"<@!{player.AssignedUser.Id}>";

                if (!player.IsAlive)
                    value = $"~~{value}~~";
            }

            return (name, value);
        }

        /// <summary>
        /// checks if the local data matches with the compared values
        /// </summary>
        /// <param name="data">the values to check against</param>
        /// <returns></returns>
        public bool DataIsObsolete((string name, string value) data)
        {
            return Name != data.name || Value != data.value;
        }

        /// <summary>
        /// saves the values to local properties
        /// </summary>
        /// <param name="data"></param>
        public void UpdateData((string name, string value) data)
        {
            Name = data.name;
            Value = data.value;
        }

        /// <summary>
        /// returns true if the embed field for this player should be displayed
        /// </summary>
        /// <returns></returns>
        public bool ShouldDisplayEmbedData()
        {
            return !string.IsNullOrEmpty(Name);
        }
    }
}
