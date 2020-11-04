using DSharpPlus.Entities;
using System.Collections.Generic;
using System.Linq;

namespace Discord.Bot.AmongUs.Library
{
    /// <summary>
    /// utility methods
    /// </summary>
    public static class Utility
    {
        /// <summary>
        /// computes the index item in the given ienumerable
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <param name="field"></param>
        /// <returns>the index if the item is found, -1 otherwise</returns>
        public static int IndexOf<T>(this IEnumerable<T> list, T field) where T : class
        {
            int index = 0;
            foreach (var item in list)
            {
                if (ReferenceEquals(item, field))
                    return index;
                else
                    index++;
            }

            return -1;
        }

        /// <summary>
        /// adds the given items to the list
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list">the list to which to add</param>
        /// <param name="values">items to add</param>
        public static void AddRange<T>(this IList<T> list, params T[] values)
        {
            foreach (var value in values)
                list.Add(value);
        }

        /// <summary>
        /// gets the members of a guild, including the owner
        /// </summary>
        /// <param name="guild"></param>
        /// <returns></returns>
        public static List<DiscordMember> GetGuildMembers(this DiscordGuild guild)
        {
            var guildMembers = guild.Members.Select(x => x.Value).ToList();
            if (!guildMembers.Contains(guild.Owner))
                guildMembers.Add(guild.Owner);

            return guildMembers;
        }

    }
}
