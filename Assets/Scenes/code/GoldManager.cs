using UnityEngine;
using System;

public class GoldManager : MonoBehaviour
{
    public static GoldManager Instance { get; private set; }

    [Header("Gold Settings")]
    [SerializeField] private int startingGold = 0;

    [SerializeField] private int startingGoldBonus = 1200000;

    private const string GoldKey = "Kingdom_Gold";
    private const string BonusClaimedKey = "Kingdom_StartingGoldBonus_Claimed";

    public int Gold { get; private set; }

    private void Awake()
    {
        // Prevent duplicate GoldManagers.
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        LoadGold();
    }

    private void LoadGold()
    {
        // Load previously saved gold.
        Gold = Mathf.Max(
            0,
            PlayerPrefs.GetInt(GoldKey, startingGold)
        );

        // Add the starting bonus only once.
        if (PlayerPrefs.GetInt(BonusClaimedKey, 0) == 0)
        {
            long newGold = (long)Gold + startingGoldBonus;

            Gold = (int)Math.Min(newGold, int.MaxValue);

            PlayerPrefs.SetInt(BonusClaimedKey, 1);

            Debug.Log("Starting gold bonus added: " + startingGoldBonus);
        }

        SaveGold();

        Debug.Log("Gold loaded: " + Gold);
    }

    public bool SpendGold(int amount)
    {
        if (amount <= 0)
        {
            Debug.LogWarning("SpendGold: Amount must be greater than zero.");
            return false;
        }

        if (Gold < amount)
        {
            Debug.LogWarning("Not enough gold! Current gold: " + Gold);
            return false;
        }

        Gold -= amount;

        SaveGold();

        Debug.Log("Spent " + amount + ". Remaining gold: " + Gold);

        return true;
    }

    public void AddGold(int amount)
    {
        if (amount <= 0)
        {
            Debug.LogWarning("AddGold: Amount must be greater than zero.");
            return;
        }

        long newGold = (long)Gold + amount;
        Gold = (int)Math.Min(newGold, int.MaxValue);

        SaveGold();

        Debug.Log("Added " + amount + ". Current gold: " + Gold);
    }

    public void SetGold(int amount)
    {
        Gold = Mathf.Max(0, amount);

        SaveGold();

        Debug.Log("Gold set to: " + Gold);
    }

    public void ResetGold()
    {
        Gold = Mathf.Max(0, startingGold);

        SaveGold();

        Debug.Log("Gold reset to: " + Gold);
    }

    private void SaveGold()
    {
        PlayerPrefs.SetInt(GoldKey, Gold);
        PlayerPrefs.Save();
    }
}
