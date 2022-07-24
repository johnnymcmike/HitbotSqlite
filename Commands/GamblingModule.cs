using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity.Extensions;
using HitbotSqlite.Models;
using HitbotSqlite.Services;

namespace HitbotSqlite.Commands;

public class GamblingModule : BaseCommandModule
{
    public GamblingModule(EconService eco, Random rng)
    {
        Econ = eco;
        Rng = rng;
    }

    private EconService Econ { get; }
    private Random Rng { get; }

    [Command("slots")]
    public async Task SlotMachine(CommandContext ctx, int bet = 1)
    {
    }

    [Command("duel")]
    public async Task DuelCommand(CommandContext ctx, DiscordMember target, int bet = 0)
    {
        bet = Math.Abs(bet);
        var interactivity = ctx.Client.GetInteractivity();
        var caller = ctx.Member;
        var triumph = DiscordEmoji.FromName(ctx.Client, ":triumph:");
        if (Econ.GetBalance(caller) < bet || Econ.GetBalance(target) < bet)
        {
            await ctx.RespondAsync("Insufficient funds on one or both sides.");
            return;
        }

        var firstmsg =
            await ctx.Channel.SendMessageAsync($"Time for a duel! {target.Nickname}, react with {triumph} to accept!");
        await firstmsg.CreateReactionAsync(triumph);
        var result = await firstmsg.WaitForReactionAsync(target, triumph);

        if (result.TimedOut)
        {
            await ctx.RespondAsync("Timed out.");
            return;
        }

        int[] rnums = new int[3];
        for (int i = 0; i < 3; i++) rnums[i] = Rng.Next(1, 11);

        await ctx.Channel.SendMessageAsync("First one to say anything after I say \"GO\" wins.");
        await ctx.Channel.SendMessageAsync("Three...");
        await Task.Delay(rnums[0] * 1000);
        if (rnums[1] <= 7)
        {
            await ctx.Channel.SendMessageAsync("Two...");
            await Task.Delay(rnums[1] * 1000);
            if (rnums[2] <= 7)
            {
                await ctx.Channel.SendMessageAsync("One...");
                await Task.Delay(rnums[2] * 1000);
            }
        }

        await ctx.Channel.SendMessageAsync("GO");

        var wa = await interactivity.WaitForMessageAsync(x =>
            x.Channel.Id == ctx.Channel.Id && x.Author.Id == caller.Id || x.Author.Id == target.Id);
        var winningMessage = wa.Result;

        if (wa.TimedOut || winningMessage is null)
        {
            await ctx.RespondAsync("Nobody won. You slackers.");
            return;
        }

        if (winningMessage.Author.Id == caller.Id)
        {
            Econ.IncrementBalance(caller, bet);
            Econ.DecrementBalance(target, bet);
        }
        else
        {
            Econ.IncrementBalance(target, bet);
            Econ.DecrementBalance(caller, bet);
        }

        await ctx.Channel.SendMessageAsync($"{winningMessage.Author.Username} won!");
        await ctx.RespondAsync(
            $"Resulting balances: {Econ.GetBalance(caller)}, {Econ.GetBalance(target)}");
    }

    [Command("blackjack")]
    [Description(
        "A text-based Blackjack game, optionally including bets and multiplayer. Pays 2 to 1. Dealer draws to 16.")]
    public async Task BlackJackCommand(CommandContext ctx, string mode = "free")
    {
        var interactivity = ctx.Client.GetInteractivity();
        var myemoji = DiscordEmoji.FromName(ctx.Client, ":black_joker:");
        var entrymsg = await
            ctx.Channel.SendMessageAsync($"A {mode} game of blackjack is starting! React " +
                                         $"{myemoji} within 20 seconds to enter.");
        await entrymsg.CreateReactionAsync(myemoji);

        var reactions = await entrymsg.CollectReactionsAsync(TimeSpan.FromSeconds(20));
        var players = new HashSet<DiscordMember>();
        foreach (var reactionObject in reactions)
        {
            var reactedUser = reactionObject.Users.First();
            if (!reactedUser.Equals(ctx.Client.CurrentUser))
                players.Add((DiscordMember) reactedUser);
        }

        if (players.Count == 0)
        {
            await ctx.RespondAsync("Timed out.");
            return;
        }

        var bets = new Dictionary<DiscordUser, int>();
        int pot = 0;
        if (mode != "free")
        {
            await ctx.Channel.SendMessageAsync("Getting everyone's bets...");
            foreach (var currentPlayer in players)
            {
                int thisBet = 0;
                await ctx.Channel.SendMessageAsync($"{currentPlayer.Mention}, what's your bet?");
                var action = await interactivity.WaitForMessageAsync(x =>
                    x.Author.Equals(currentPlayer) && int.TryParse(x.Content, out thisBet));
                if (action.TimedOut) await ctx.Channel.SendMessageAsync("Timed out, defaulting to 0.");
                thisBet = Math.Abs(thisBet);
                if (thisBet > Econ.GetBalance(currentPlayer))
                {
                    thisBet = Econ.GetBalance(currentPlayer) ?? 0;
                    await ctx.Channel.SendMessageAsync(
                        $"You tried to bet more than you have, so you'll be betting {thisBet} kromer, which is all you have. :)");
                }

                bets.Add(currentPlayer, thisBet);
                Econ.DecrementBalance(currentPlayer, thisBet);
            }

            pot += bets.Sum(x => x.Value);
        }

        var deck = new DeckOfCards();
        deck.Shuffle();

        //Dictionary where the keys are the DiscordUsers we just got, and the values start as an empty card list
        var playerHands = players.ToDictionary(x => x, _ => new BlackJackHand());
        //Draw for the dealer
        var dealerHand = new BlackJackHand();
        dealerHand.Cards = new List<PlayingCard>
        {
            deck.DrawCard(),
            deck.DrawCard()
        };
        //Deal first two cards and tell people their hands
        foreach (var (key, value) in playerHands)
        {
            value.Cards.Add(deck.DrawCard());
            value.Cards.Add(deck.DrawCard());
            await key.SendMessageAsync($"Your hand is: {value}\n Your hand's value is {value.GetHandValue()}");
        }

        //Announce everyone's first card
        string firstCardAnnounce = "Here is everyone's first card:\n";
        firstCardAnnounce += $"Dealer: {dealerHand.Cards[0]}\n";
        foreach (var (key, value) in playerHands) firstCardAnnounce += $"{key.DisplayName}: {value.Cards[0]}\n";
        await ctx.Channel.SendMessageAsync(firstCardAnnounce);
        //Check for blackjacks
        // var blackJackedUsers = new List<DiscordMember>();
        // foreach (var (key, value) in playerHands) //this mess checks if you have a face and an ace with NO tens
        //     if (value.Cards.Exists(x => x.BlackJackValue == 1) && !value.Cards.Exists(x => x.Num == CardNumber.Ten) &&
        //         value.Cards.Exists(x => x.BlackJackValue == 10))
        //     {
        //         blackJackedUsers.Add(key);
        //         await ctx.Channel.SendMessageAsync($"{key.DisplayName} got a blackjack!");
        //     }
        //
        // if (blackJackedUsers.Count >= 1)
        // {
        //     //TODO: what to do if multiple get blackjack, and implement checking for dealer blackjack
        //     await ctx.Channel.SendMessageAsync(
        //         "Exiting prematurely because 1 or more users got a blackjack. Remind me to finish this later");
        //     return;
        // }

        await ctx.Channel.SendMessageAsync("-----------------------------------------");

        //Begin turns
        string[] possibleActions = {"hit", "stand"};

        await ctx.Channel.SendMessageAsync("When it's your turn, say \"hit\" to hit, and \"stand\" to stand.");
        foreach (var currentPlayer in players)
        {
            bool turnOver = false;
            await ctx.Channel.SendMessageAsync($"{currentPlayer.DisplayName}'s turn!");

            while (!turnOver)
            {
                var action = await interactivity.WaitForMessageAsync(x =>
                    x.Author.Equals(currentPlayer) && possibleActions.Contains(x.Content.ToLower()));
                if (action.TimedOut)
                {
                    await ctx.Channel.SendMessageAsync(
                        "Someone's turn timed out, so we're defaulting to stand.");
                    turnOver = true;
                    continue;
                }

                string lala = action.Result.Content.ToLower();
                if (lala == "hit")
                {
                    await ctx.Channel.SendMessageAsync("Hitting...");
                    var drawncard = deck.DrawCard();
                    playerHands[currentPlayer].Cards.Add(drawncard);
                    await currentPlayer.SendMessageAsync(
                        $"You got the {drawncard}. Your new hand value is {playerHands[currentPlayer].GetHandValue()}.");

                    if (playerHands[currentPlayer].GetHandValue() > 21)
                    {
                        await ctx.Channel.SendMessageAsync("You busted! Next turn...");
                        break;
                    }
                }
                else if (lala == "stand")
                {
                    turnOver = true;
                }
            }
        }

        await Task.Delay(2000);
        //Play dealer's turn
        await ctx.Channel.SendMessageAsync("My turn.");
        bool dealerBust = false;
        while (dealerHand.GetHandValue() < 16)
        {
            await ctx.Channel.SendMessageAsync("I'm hitting...");
            await Task.Delay(1000);
            var drawnCard = deck.DrawCard();
            dealerHand.Cards.Add(drawnCard);
            if (dealerHand.GetHandValue() > 21)
            {
                await ctx.Channel.SendMessageAsync("I busted :(");
                dealerBust = true;
                break;
            }
        }

        if (!dealerBust)
            await ctx.Channel.SendMessageAsync("I stand.");
        await Task.Delay(2000);
        //Print out everyone's hands
        string everyhand = "```Here's everyone's final hand.\n";
        foreach (var (key, value) in playerHands)
        {
            everyhand += $"-----------{key.DisplayName} had:\n";
            everyhand += value;
            everyhand += $"...with a value of {value.GetHandValue()}\n";
        }

        everyhand += "-----------Dealer had:\n" + dealerHand;
        everyhand += $"...with a value of {dealerHand.GetHandValue()}```";
        await ctx.Channel.SendMessageAsync(everyhand);
        await Task.Delay(2000);
        //Determine winner
        playerHands.Add(ctx.Guild.CurrentMember, dealerHand);
        DiscordMember? currentWinner = null;
        var duplicateScores = new Dictionary<DiscordMember, BlackJackHand>();
        foreach (var (dictkey, dictvalue) in playerHands)
        {
            bool justSet = false;
            if (dictvalue.GetHandValue() > 21)
                continue;

            if (currentWinner is null)
            {
                currentWinner = dictkey;
                justSet = true;
            }

            if (playerHands[currentWinner].GetHandValue() < dictvalue.GetHandValue())
            {
                currentWinner = dictkey;
                justSet = true;
            }
            else if (playerHands[currentWinner].GetHandValue() == dictvalue.GetHandValue() && !justSet)
            {
                if (!duplicateScores.ContainsKey(currentWinner))
                    duplicateScores.Add(currentWinner, playerHands[currentWinner]);
                if (!duplicateScores.ContainsKey(dictkey))
                    duplicateScores.Add(dictkey, dictvalue);
            }
        }

        if (currentWinner is null)
        {
            await ctx.Channel.SendMessageAsync("nobody won lol");
            return;
        }

        if (duplicateScores.Count > 1 && duplicateScores.ContainsKey(currentWinner))
        {
            var realWinner = currentWinner;
            foreach (var (key, value) in duplicateScores)
                if (value.GetHandWeight() > duplicateScores[realWinner].GetHandWeight())
                {
                    realWinner = key;
                }
                else if (value.GetHandWeight() == duplicateScores[realWinner].GetHandWeight() &&
                         !duplicateScores[realWinner].Equals(value))
                {
                    await ctx.Channel.SendMessageAsync("There was a *true* tie, so all tied players win.");
                    foreach (var (winner, _) in duplicateScores)
                        if (!winner.Equals(ctx.Guild.CurrentMember))
                            Econ.IncrementBalance(winner,
                                (int) (2 * pot * ((float) bets[winner] / pot)));
                    return;
                }

            currentWinner = realWinner;
        }

        if (currentWinner.Equals(ctx.Guild.CurrentMember))
        {
            await ctx.Channel.SendMessageAsync("the house won ;)");
            return;
        }

        if (mode == "free" || pot == 0)
        {
            await ctx.Channel.SendMessageAsync(
                $"{currentWinner.Mention} won! :)");
        }
        else
        {
            int payout =
                (int) (2 * pot * ((float) bets[currentWinner] / pot));
            Econ.IncrementBalance(currentWinner, payout);
            await ctx.Channel.SendMessageAsync(
                $"{currentWinner.Mention} won, net-gaining {payout - bets[currentWinner]} kromer!");
        }
    }
}