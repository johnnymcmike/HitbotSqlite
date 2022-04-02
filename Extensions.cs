using DSharpPlus.Entities;

namespace HitbotSqlite;

public static class Extensions
{
    public static string GetTag(this DiscordMember member)
    {
        return member.Username + "#" + member.Discriminator;
    }
}