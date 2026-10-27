
using UnityEngine;
using UnityEngine.UI;

public class BlackjackCardSlot : MonoBehaviour
{
    [Header("Card References")]
    [SerializeField] private Image cardImage;
    [SerializeField] private BlackjackCardLibrary cardLibrary;

    private int cardIndex = -1;
    private bool faceUp = false;

    private void Awake()
    {
        if (cardImage == null)
        {
            cardImage = GetComponent<Image>();
        }

        ShowEmpty();
    }

    // Hide the card slot when starting a new round.
    public void ShowEmpty()
    {
        cardIndex = -1;
        faceUp = false;

        if (cardImage != null)
        {
            cardImage.sprite = null;
            cardImage.enabled = false;
        }
    }

    // Display a card with its back visible.
    public void ShowFaceDown(int index)
    {
        if (!IsValidCard(index))
            return;

        cardIndex = index;
        faceUp = false;

        RefreshCard();
    }

    // Display a card face-up.
    public void ShowFaceUp(int index)
    {
        if (!IsValidCard(index))
            return;

        cardIndex = index;
        faceUp = true;

        RefreshCard();
    }

    // Flip an existing face-down card.
    public void FlipFaceUp()
    {
        if (cardIndex < 0)
            return;

        faceUp = true;
        RefreshCard();
    }

    private bool IsValidCard(int index)
    {
        if (cardLibrary == null)
        {
            Debug.LogError(
                "BlackjackCardLibraryAsset has not been assigned on " +
                gameObject.name
            );
            return false;
        }

        if (index < 0 || index >= 52)
        {
            Debug.LogError(
                "Card index must be between 0 and 51."
            );
            return false;
        }

        if (cardLibrary.GetFaceCard(index) == null)
        {
            Debug.LogError(
                "Missing card sprite at index " + index
            );
            return false;
        }

        return true;
    }

    private void RefreshCard()
    {
        if (cardImage == null || cardLibrary == null)
            return;

        Sprite spriteToShow;

        if (faceUp)
        {
            spriteToShow = cardLibrary.GetFaceCard(cardIndex);
        }
        else
        {
            spriteToShow = cardLibrary.cardBack;
        }

        cardImage.sprite = spriteToShow;
        cardImage.enabled = spriteToShow != null;
    }
}
