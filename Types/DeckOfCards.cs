namespace HitbotSqlite.Models;

public class DeckOfCards
{
    public List<PlayingCard> Cards;
    private readonly Random rng;

    public DeckOfCards()
    {
        Cards = new List<PlayingCard>();
        Reset();
        rng = new Random();
    }

    private void Reset()
    {
        Cards.Clear();
        for (int i = 0; i < 4; i++)
        for (int j = 1; j < 14; j++)
            Cards.Add(new PlayingCard {Suit = (CardSuit) i, Num = (CardNumber) j});
    }

    /// <summary>
    ///     Randomly shuffles the order of elements in Cards.
    /// </summary>
    public void Shuffle()
    {
        var shuffledcards = Cards.OrderBy(a => rng.Next()).ToList();
        Cards = shuffledcards;
    }

    public PlayingCard DrawCard()
    {
        if (Cards.Count == 0)
            throw new Exception("Tried to draw from an empty deck of cards.");

        var result = Cards[0];
        Cards.RemoveAt(0);
        return result;
    }
}