namespace HitbotSqlite.Models;

public class BlackJackHand
{
    public BlackJackHand()
    {
        Clear();
    }

    public List<PlayingCard> Cards { get; set; }

    public int GetHandValue()
    {
        int val = 0;
        var aces = new List<PlayingCard>();
        foreach (var card in Cards)
        {
            if (card.BlackJackValue == 1)
            {
                aces.Add(card);
                continue;
            }

            val += card.BlackJackValue;
        }

        foreach (var ace in aces)
            if (val + 11 > 21)
                val += 1;
            else
                val += 11;

        return val;
    }

    /// <summary>
    ///     Gets the "weight" of the hand for the purpose of breaking ties. Aces are always worth 11.
    /// </summary>
    /// <returns></returns>
    public int GetHandWeight()
    {
        int val = 0;
        foreach (var card in Cards)
            if (card.Num == CardNumber.Ace)
                val += 11;
            else
                val += (int) card.Num;

        return val;
    }

    public void Clear()
    {
        Cards = new List<PlayingCard>();
    }

    public override string ToString()
    {
        string result = "";
        foreach (var card in Cards) result += card + "\n";

        return result;
    }
}