namespace HitbotSqlite.Models;

public class Guild
{
    public ulong GuildId { get; set; }

    public int? LottoPot { get; set; }

    public List<Member> Members { get; set; } = new();
}