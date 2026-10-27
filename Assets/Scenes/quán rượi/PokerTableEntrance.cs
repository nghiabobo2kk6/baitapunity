
using UnityEngine;

public class PokerTableEntrance : MonoBehaviour
{
    [Header("Poker UI")]
    [SerializeField] private GameObject pokerCanvas;

    private void Start()
    {
        if (pokerCanvas != null)
        {
            pokerCanvas.SetActive(false);
        }
        else
        {
            Debug.LogError(
                "Poker Canvas is not assigned on PokerTable!"
            );
        }
    }

    private void OnMouseDown()
    {
        if (pokerCanvas == null)
            return;

        pokerCanvas.SetActive(true);

        Debug.Log("Poker table clicked. Poker UI opened.");
    }

    public void ClosePoker()
    {
        if (pokerCanvas != null)
            pokerCanvas.SetActive(false);
    }
}
