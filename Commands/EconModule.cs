using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity.Extensions;
using HitbotSqlite.Services;

namespace HitbotSqlite.Commands;

[RequireGuild]
public class EconModule : BaseCommandModule
{
    private EconService Econ { get; }

    public EconModule(EconService econ)
    {
        Econ = econ;
    }

    [Command("register")]
    public async Task RegisterCommand(CommandContext ctx)
    {
        if (ctx.Member is null) throw new ArgumentNullException(nameof(ctx.Member), "Caller was somehow null.");

        switch (Econ.RegisterMember(ctx.Member, ctx.Guild))
        {
            case -1:
                await ctx.RespondAsync("You are already registered.");
                break;
            case 1:
                await ctx.RespondAsync("You have been registered.");
                break;
        }
    }

    [Command("balance")]
    public async Task BalanceCommand(CommandContext ctx)
    {
        if (ctx.Member is null) throw new ArgumentNullException(nameof(ctx.Member), "Caller was somehow null.");

        int? result = Econ.GetBalance(ctx.Member, ctx.Guild);
        if (result is null)
            await ctx.RespondAsync("You are not registered in this server.");
        else
            await ctx.RespondAsync($"You have {result} coins.");
    }

    [Command("balance")]
    public async Task BalanceCommand(CommandContext ctx, DiscordMember membertocheck)
    {
        int? result = Econ.GetBalance(membertocheck, ctx.Guild);
        if (result is null)
            await ctx.RespondAsync("This user is not registered in this server.");
        else
            await ctx.RespondAsync($"{membertocheck.DisplayName} has {result} coins.");
    }

    [Command("pay")]
    public async Task PayCommand(CommandContext ctx, DiscordMember recipient, int amount)
    {
        var caller = ctx.Member;
        if (caller is null) throw new ArgumentNullException(nameof(ctx.Member), "Caller was somehow null.");

        var guild = ctx.Guild;

        int? callerBalance = Econ.GetBalance(caller, guild);

        if (callerBalance is null)
        {
            await ctx.RespondAsync("You are not registered in this server.");
            return;
        }

        if (callerBalance < amount)
        {
            await ctx.RespondAsync("You don't have enough coins.");
            return;
        }

        //Decrement caller's balance
        Econ.DecrementBalance(caller, ctx.Guild, amount);
        //Increment recipient's balance
        Econ.IncrementBalance(recipient, ctx.Guild, amount);
        //Send message with resulting balances of both parties
        await ctx.RespondAsync(
            $"Paid {amount} coins to {recipient.DisplayName}, leaving you with " +
            $"{Econ.GetBalance(caller, guild)} and them with " +
            $"{Econ.GetBalance(recipient, guild)}.");
    }

    [Command("leaderboard")]
    public async Task LeaderboardCommand(CommandContext ctx)
    {
        var board = Econ.GetLeaderboard(ctx.Guild);
        var interactivity = ctx.Client.GetInteractivity();
        string result = "";
        int i = 1;
        foreach (var member in board) result += $"{i}. {member.Tag} with {member.EconBalance} coins\n";

        var pages = interactivity.GeneratePagesInEmbed(result);
        await ctx.Channel.SendPaginatedMessageAsync(ctx.Member, pages);
    }

    [Command("print")]
    [RequireOwner]
    public async Task PrintCommand(CommandContext ctx, DiscordMember recipient, int amount)
    {
        //Print new currency and give to recipient
        Econ.IncrementBalance(recipient, ctx.Guild, amount);
        await ctx.RespondAsync(
            $"{amount} coins have been printed to {recipient.DisplayName}, leaving them with {Econ.GetBalance(recipient, ctx.Guild)}.");
    }

    [Command("turgle")]
    public async Task TurgleCommand(CommandContext ctx, int amount = 100000)
    {
        var caller = ctx.Member;
        if (caller is null) throw new ArgumentNullException(nameof(ctx.Member), "Caller was somehow null.");

        Econ.DecrementBalance(caller, ctx.Guild, amount);
        await ctx.RespondAsync(
            $"You turgled away {amount} coins, leaving you with {Econ.GetBalance(caller, ctx.Guild)}.");
    }
}