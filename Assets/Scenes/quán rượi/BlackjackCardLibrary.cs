
using UnityEngine;

[CreateAssetMenu(
    fileName = "BlackjackCardLibraryAsset",
    menuName = "Blackjack/Card Library")]
public class BlackjackCardLibrary : ScriptableObject
{
    [Header("52 face-up card sprites")]
    public Sprite[] faceCards = new Sprite[52];

    [Header("Face-down card sprite")]
    public Sprite cardBack;

    public Sprite GetFaceCard(int index)
    {
        if (faceCards == null || index < 0 || index >= faceCards.Length)
        {
            Debug.LogError("Card index must be between 0 and 51.");
            return null;
        }

        return faceCards[index];
    }
}
