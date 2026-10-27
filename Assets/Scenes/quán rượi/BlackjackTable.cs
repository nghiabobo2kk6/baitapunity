
using UnityEngine;

public class BlackjackTable : MonoBehaviour
{
    [Header("Blackjack UI")]
    [SerializeField] private GameObject blackjackCanvas;

    private void Start()
    {
        if (blackjackCanvas != null)
        {
            blackjackCanvas.SetActive(false);
        }
        else
        {
            Debug.LogError(
                "Chua gan BlackjackCanvas vao BlackjackTable!",
                this
            );
        }
    }

    private void OnMouseDown()
    {
        if (blackjackCanvas != null)
        {
            blackjackCanvas.SetActive(true);
        }
    }

    public void CloseBlackjack()
    {
        if (blackjackCanvas != null)
        {
            blackjackCanvas.SetActive(false);
        }
    }
}
