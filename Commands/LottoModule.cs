namespace HitbotSqlite.Commands;

[Group("lotto")]
public class LottoModule : BaseCommandModule
{
    private EconService Econ{ get; set; }
    private Random Rng { get; set; }
    
    public LottoModule(EconService eco, Random rn)
    {
        Econ = eco;
        Rng = rn;
    }

    [Command("buyticket")]
    public async Task LottoBuyTicketCommand(CommandContext ctx)
    {
        if(Econ.EnterUserInLotto(ctx.Member))
        {
            await ctx.RespondAsync("Entered :)");
        }
    }

    [Command("view")]
    public async Task LottoViewCommand(CommandContext ctx)
    {
        var lotto = Econ.GetUsersInLotto(ctx.Guild);
        if (lotto is null)
        {
            await ctx.RespondAsync("Nobody is entered.");
            return;
        }
        var interactivity = ctx.Client.GetInteractivity();
        string result = $"The pot is {Econ.GetLottoPot(ctx.Guild)}, and the following users are entered:\n";
        foreach (var entrant in lotto)
        {
            result += entrant.Tag + "\n";
        }
        var pages = interactivity.GeneratePagesInEmbed(result);
        await ctx.Channel.SendPaginatedMessageAsync(ctx.Member, pages);
    }
    
    [Command("draw")]
    public async Task LottoDrawCommand(CommandContext ctx)
    {
        var result = Econ.DrawLotto(ctx.Guild);
        if (result is null)
        {
            await ctx.RespondAsync("Draw failed. Either not enough people are entered, or the lottery has already been drawn today.");
            return;
        }
        var dMember = await ctx.Guild.GetMemberAsync(result.DiscordMemberId);
        await ctx.RespondAsync($"{dMember.Mention} won, leaving them with {result.EconBalance} coins!");
    }
}