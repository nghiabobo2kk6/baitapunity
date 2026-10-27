
using UnityEngine;

[CreateAssetMenu(
    fileName = "PokerDeck",
    menuName = "Poker/Create Deck")]
public class PokerDeck : ScriptableObject
{
    [Header("52 Playing Cards")]
    public Sprite[] cards = new Sprite[52];

    [Header("Card Back")]
    public Sprite cardBack;

    private void OnValidate()
    {
        if (cards == null || cards.Length != 52)
        {
            System.Array.Resize(ref cards, 52);
        }
    }
}
