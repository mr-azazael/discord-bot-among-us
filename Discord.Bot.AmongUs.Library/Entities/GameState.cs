namespace Discord.Bot.AmongUs.Library.Entities
{
    /// <summary>
    /// the state of the lobby
    /// </summary>
    enum GameState
    {
        /// <summary>
        /// lobby was just created
        /// </summary>
        StartUp,
        /// <summary>
        /// ready for players to join
        /// </summary>
        InLobby,
        /// <summary>
        /// game started
        /// </summary>
        Running,
        /// <summary>
        /// game is paused
        /// </summary>
        Paused,
        /// <summary>
        /// lobby is closing
        /// </summary>
        Finished
    }
}
