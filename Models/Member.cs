namespace HitbotSqlite.Models;

public class Member
{
    //identification
    [Key] public string MemberId { get; set; }
    public ulong DiscordMemberId { get; set; }
    
    //data
    public int? EconBalance { get; set; }
    public string? Tag { get; set; }
    public bool IsInLotto { get; set; }
    public DateTime LastClaimedDaily { get; set; }
    
    //relations
    public Guild Guild { get; set; }
    public ulong GuildId { get; set; }
}