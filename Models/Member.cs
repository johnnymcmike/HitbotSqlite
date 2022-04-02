using System.ComponentModel.DataAnnotations;

namespace HitbotSqlite.Models;

public class Member
{
    [Key] public string MemberId { get; set; }
    public ulong DiscordMemberId { get; set; }

    public int? EconBalance { get; set; }
    public string? Tag { get; set; }

    public Guild Guild { get; set; }
    public ulong GuildId { get; set; }
}