using DSharpPlus;
using DSharpPlus.Entities;
using System.Linq;

namespace Discord.Bot.AmongUs.Library
{
    class PermissionsManager
    {
        public static bool UserHasPermission(DiscordGuild guild, DiscordUser user, Permissions permissions)
        {
            return guild.GetGuildMemberForUser(user).Roles.Any(x => x.CheckPermission(permissions) == PermissionLevel.Allowed);
        }
    }
}
