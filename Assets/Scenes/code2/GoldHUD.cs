
using UnityEngine;
using TMPro;
using System.Collections;

public class GoldHUD : MonoBehaviour
{
    [Header("Gold UI")]
    [SerializeField] private TMP_Text goldText;

    private GoldManager goldManager;
    private int lastDisplayedGold = -1;
    private Coroutine initializationCoroutine;

    private void OnEnable()
    {
        lastDisplayedGold = -1;

        if (initializationCoroutine != null)
        {
            StopCoroutine(initializationCoroutine);
        }

        initializationCoroutine = StartCoroutine(InitializeHUD());
    }

    private void OnDisable()
    {
        if (initializationCoroutine != null)
        {
            StopCoroutine(initializationCoroutine);
            initializationCoroutine = null;
        }
    }

    private IEnumerator InitializeHUD()
    {
        // Wait for GoldManager to initialize.
        while (GoldManager.Instance == null)
        {
            yield return null;
        }

        goldManager = GoldManager.Instance;

        UpdateGoldDisplay();

        initializationCoroutine = null;
    }

    private void Update()
    {
        // Detect a changed or recreated GoldManager.
        if (goldManager != GoldManager.Instance)
        {
            goldManager = GoldManager.Instance;
            lastDisplayedGold = -1;
        }

        UpdateGoldDisplay();
    }

    public void UpdateGoldDisplay()
    {
        if (goldText == null)
        {
            return;
        }

        if (GoldManager.Instance == null)
        {
            return;
        }

        goldManager = GoldManager.Instance;

        int currentGold = goldManager.Gold;

        if (currentGold == lastDisplayedGold)
        {
            return;
        }

        lastDisplayedGold = currentGold;

        // Display numbers only, with thousands separators.
        goldText.text = currentGold.ToString("N0");

        Debug.Log("GoldHUD displayed: " + currentGold);
    }

    public void RefreshGold()
    {
        lastDisplayedGold = -1;
        UpdateGoldDisplay();
    }
}
