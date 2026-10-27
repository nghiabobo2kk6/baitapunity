
using UnityEngine;
using UnityEngine.UI;

public class SicBoBetButton : MonoBehaviour
{
    [Header("Bet Settings")]
    [SerializeField] private string betType;

    [Header("References")]
    [SerializeField] private Button button;
    [SerializeField] private SicBoBettingManager bettingManager;

    private void Awake()
    {
        if (button == null)
        {
            button = GetComponent<Button>();
        }

        if (button != null)
        {
            button.onClick.RemoveListener(OnBetClicked);
            button.onClick.AddListener(OnBetClicked);
        }
        else
        {
            Debug.LogError(
                "Khong tim thay Button tren: " + gameObject.name
            );
        }
    }

    private void OnDestroy()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(OnBetClicked);
        }
    }

    private void OnBetClicked()
    {
        if (bettingManager == null)
        {
            Debug.LogError(
                "Chua gan SicBoBettingManager cho: " + gameObject.name
            );
            return;
        }

        if (string.IsNullOrWhiteSpace(betType))
        {
            Debug.LogError(
                "Bet Type dang trong tren: " + gameObject.name
            );
            return;
        }

        bettingManager.SelectBet(betType.Trim(), 0);
    }
}
