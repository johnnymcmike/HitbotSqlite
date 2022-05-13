using DSharpPlus.Entities;
using HitbotSqlite.DataAccess;
using HitbotSqlite.Models;
using Microsoft.EntityFrameworkCore;

namespace HitbotSqlite.Services;

public class EconService
{
    public EconContext Db { get; }

    public EconService(EconContext eco)
    {
        Db = eco;
    }

    /// <summary>
    ///     Attempts to register a new member to the database.
    /// </summary>
    /// <param name="member">Discord Id of the member.</param>
    /// <returns>-1 if member is already registered. 1 on success.</returns>
    public int RegisterMember(DiscordMember member)
    {
        //write down some shorthand
        var guild = member.Guild;
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

    public int? GetBalance(DiscordMember member)
    {
        var guild = member.Guild;
        ulong memberId = member.Id;
        ulong guildId = guild.Id;
        var memberToGet = Db.Members.Find(IdsToMemberKey(memberId, guildId));
        return memberToGet?.EconBalance;
    }

    /// <summary>
    ///     Increments the balance of a member by the specified amount. Will always be by a positive amount.
    /// </summary>
    /// <param name="member">Discord ID of the member whose balance is to be incremented.</param>
    /// <param name="by">Amount to increment the balance by, defaulting to 1.</param>
    public void IncrementBalance(DiscordMember member, int @by = 1)
    {
        var guild = member.Guild;
        ulong memberId = member.Id;
        ulong guildId = guild.Id;

        by = Math.Abs(by);
        string memberkey = IdsToMemberKey(memberId, guildId);
        var memberToInc = Db.Members.Find(memberkey);
        if (memberToInc is null)
        {
            RegisterMember(member);
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
    /// <param name="by">Amount to decrement the balance by, defaulting to 1.</param>
    public void DecrementBalance(DiscordMember member, int @by = 1)
    {
        var guild = member.Guild;
        ulong memberId = member.Id;
        ulong guildId = guild.Id;
        by = Math.Abs(by);
        string memberkey = IdsToMemberKey(memberId, guildId);
        var memberToDec = Db.Members.Find(memberkey);
        if (memberToDec is null)
        {
            RegisterMember(member);
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

    public int UserClaimDaily(DiscordMember member)
    {
        //Increment the balance of the member by 10 if it is not already claimed today
        var guild = member.Guild;
        var memberClaiming = Db.Members.Find(IdsToMemberKey(member.Id, guild.Id));
        if (memberClaiming is null)
        {
            return -1;
        }

        var lastClaimed = memberClaiming.LastClaimedDaily;
        if (lastClaimed.Day != DateTime.Now.Day)
        {
            IncrementBalance(member, 10);
            memberClaiming.LastClaimedDaily = DateTime.Now;
            Db.SaveChanges();
            return 1;
        }

        return 0;
    }

    public bool EnterUserInLotto(DiscordMember member)
    {
        var guild = member.Guild;
        var memberClaiming = Db.Members.Find(IdsToMemberKey(member.Id, guild.Id));
        if (memberClaiming is null || memberClaiming.EconBalance < 10 || memberClaiming.IsInLotto)
        {
            return false;
        }

        DecrementBalance(member, 10);
        Db.Guilds.Find(guild.Id)!.LottoPot += 10;
        memberClaiming.IsInLotto = true;
        return true;
    }

    public int GetLottoPot(DiscordGuild guild)
    {
        var guildToGet = Db.Guilds.Find(guild.Id);
        if (guildToGet is null)
        {
            return -1;
        }

        return guildToGet.LottoPot;
    }

    public void ResetLotto(DiscordGuild guild)
    {
        var guildToReset = Db.Guilds.Find(guild.Id);
        if (guildToReset is null)
        {
            return;
        }

        guildToReset.LottoPot = 0;
        foreach (var member in guildToReset.Members)
        {
            member.IsInLotto = false;
        }

        Db.SaveChanges();
    }

    public Member? DrawLotto(DiscordGuild guild)
    {
        var rng = new Random();
        var guildToDraw = Db.Guilds.Find(guild.Id);
        if (guildToDraw is null || guildToDraw.LottoLastDrawn.Day == DateTime.Now.Day)
        {
            return null;
        }

        var players = GetUsersInLotto(guild)?.ToArray();
        if (players is null || players.Length <= 1)
        {
            return null;
        }

        var winner = players[rng.Next(0, players.Length)];
        winner.EconBalance += guildToDraw.LottoPot;
        ResetLotto(guild);
        guildToDraw.LottoLastDrawn = DateTime.Now;
        return winner;
    }

    public IEnumerable<Member>? GetUsersInLotto(DiscordGuild guild)
    {
        var lottoGuild = Db.Guilds.Find(guild.Id);
        return lottoGuild?.Members.Where(x => x.IsInLotto);
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