using DSharpPlus.Entities;
using HitbotSqlite.DataAccess;
using HitbotSqlite.Models;
using Microsoft.EntityFrameworkCore;

namespace HitbotSqlite.Services;

public class EconService
{
    private EconContext Db { get; }

    public EconService(EconContext eco)
    {
        Db = eco;
    }

    /// <summary>
    ///     Attempts to register a new member to the database.
    /// </summary>
    /// <param name="member">Discord Id of the member.</param>
    /// <param name="guild"></param>
    /// <returns>-1 if member is already registered. 1 on success.</returns>
    public int RegisterMember(DiscordMember member, DiscordGuild guild)
    {
        //write down some shorthand
        ulong memberId = member.Id;
        ulong guildId = guild.Id;
        string memberkey = IdsToMemberKey(memberId, guildId);

        //get member and guild from db
        var queriedMember = Db.Members.Find(memberkey);
        var queriedGuild = Db.Guilds.Find(guildId);

        //if the guild is not registered, register it and the member
        if (queriedGuild is null)
        {
            //make our new objects with the relevant information
            var newMember = new Member
            {
                GuildId = guildId,
                MemberId = memberkey,
                DiscordMemberId = memberId,
                EconBalance = 10,
                Tag = member.GetTag()
            };
            var newGuild = new Guild
            {
                GuildId = guildId,
                Members = new List<Member>(),
                LottoPot = 0
            };
            //link them up
            newGuild.Members.Add(newMember);
            newMember.Guild = newGuild;
            //save to db
            Db.Members.Add(newMember);
            Db.Guilds.Add(newGuild);
            Db.SaveChanges();
            return 1;
        }

        //if the member is not registered but the guild is, register them in the current guild
        if (queriedMember is null)
        {
            //make our new object with the relevant information
            var newMember = new Member
            {
                GuildId = guildId,
                MemberId = memberkey,
                DiscordMemberId = memberId,
                EconBalance = 10,
                Tag = member.GetTag(),
                //link them up
                Guild = queriedGuild
            };
            queriedGuild.Members.Add(newMember);
            //save to db
            Db.Members.Add(newMember);
            Db.SaveChanges();
            return 1;
        }

        //otherwise, both the guild and member are already registered, so return -1
        return -1;
    }

    public int? GetBalance(DiscordMember member, DiscordGuild guild)
    {
        ulong memberId = member.Id;
        ulong guildId = guild.Id;
        var memberToGet = Db.Members.Find(IdsToMemberKey(memberId, guildId));
        return memberToGet?.EconBalance;
    }

    /// <summary>
    ///     Increments the balance of a member by the specified amount. Will always be by a positive amount.
    /// </summary>
    /// <param name="member">Discord ID of the member whose balance is to be incremented.</param>
    /// <param name="guild">Discord ID of the guild the member is in.</param>
    /// <param name="by">Amount to increment the balance by, defaulting to 1.</param>
    public void IncrementBalance(DiscordMember member, DiscordGuild guild, int by = 1)
    {
        ulong memberId = member.Id;
        ulong guildId = guild.Id;

        by = Math.Abs(by);
        string memberkey = IdsToMemberKey(memberId, guildId);
        var memberToInc = Db.Members.Find(memberkey);
        if (memberToInc is null)
        {
            RegisterMember(member, guild);
            memberToInc = Db.Members.Find(memberkey);
        }

        //kinda weird but it works?
        if (memberToInc is not null)
        {
            memberToInc.EconBalance += by;
            Db.SaveChanges();
        }
    }

    /// <summary>
    ///     Decrements the balance of a member by the specified amount. Will always be by a negative amount. Decrementing will
    ///     never put a balance below zero.
    /// </summary>
    /// <param name="member">Discord ID of the member whose balance is to be incremented.</param>
    /// <param name="guild">Discord ID of the guild the member is in.</param>
    /// <param name="by">Amount to decrement the balance by, defaulting to 1.</param>
    public void DecrementBalance(DiscordMember member, DiscordGuild guild, int by = 1)
    {
        ulong memberId = member.Id;
        ulong guildId = guild.Id;
        by = Math.Abs(by);
        string memberkey = IdsToMemberKey(memberId, guildId);
        var memberToDec = Db.Members.Find(memberkey);
        if (memberToDec is null)
        {
            RegisterMember(member, guild);
            memberToDec = Db.Members.Find(memberkey);
        }

        //kinda weird but it works?
        if (memberToDec is not null)
        {
            memberToDec.EconBalance -= by;
            if (memberToDec.EconBalance < 0) memberToDec.EconBalance = 0;
            Db.SaveChanges();
        }
    }

    public IEnumerable<Member> GetLeaderboard(DiscordGuild guild)
    {
        var query = Db.Members
            .AsNoTracking()
            .Where(m => m.GuildId == guild.Id)
            .OrderBy(m => m.EconBalance);

        return query;
    }

    private static string IdsToMemberKey(ulong memberId, ulong guildId)
    {
        return $"{guildId}-{memberId}";
    }
}