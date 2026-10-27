
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SicBoBettingManager : MonoBehaviour
{
    [Header("Main UI")]
    [SerializeField] private GameObject sicBoCanvas;
    [SerializeField] private GameObject startSicBoButton;
    [SerializeField] private Button leaveSicBoButton;
    [SerializeField] private Button placeBetButton;

    [Header("Betting UI")]
    [SerializeField] private TMP_InputField betAmountInput;
    [SerializeField] private TMP_Text selectedBetText;
    [SerializeField] private TMP_Text totalBetText;
    [SerializeField] private TMP_Text goldText;
    [SerializeField] private TMP_Text dealerDialogueText;
    [SerializeField] private TMP_Text countdownText;

    [Header("Result Dice")]
    [SerializeField] private Image resultDice1;
    [SerializeField] private Image resultDice2;
    [SerializeField] private Image resultDice3;

    [Header("Dice Face Sprites")]
    [SerializeField] private Sprite diceFace1;
    [SerializeField] private Sprite diceFace2;
    [SerializeField] private Sprite diceFace3;
    [SerializeField] private Sprite diceFace4;
    [SerializeField] private Sprite diceFace5;
    [SerializeField] private Sprite diceFace6;

    [Header("Dice Animation")]
    [SerializeField] private GameObject diceRollAnimationObject;
    [SerializeField] private Animator diceRollAnimator;
    [SerializeField] private string rollAnimationState = "DiceRollAnimation";
    [SerializeField] private float delayBeforeAnimation = 2f;
    [SerializeField] private float diceVisibleDuration = 0.5f;
    [SerializeField] private float animationTimeout = 10f;

    [Header("Payout Multipliers")]
    [SerializeField] private int tripleNumberMultiplier = 30;

    // Each selected door receives the same stake amount.
    private readonly HashSet<string> selectedBets =
        new HashSet<string>();

    // Actual amount placed on each door for this round.
    private readonly Dictionary<string, int> roundBets =
        new Dictionary<string, int>();

    private bool sessionActive;
    private bool bettingOpen;
    private bool roundRunning;
    private int totalBet;
    private Coroutine roundCoroutine;

    private void Awake()
    {
        if (placeBetButton != null)
            placeBetButton.onClick.AddListener(ConfirmBet);

        if (leaveSicBoButton != null)
            leaveSicBoButton.onClick.AddListener(LeaveSicBo);

        HideDice();

        if (diceRollAnimationObject != null)
            diceRollAnimationObject.SetActive(false);

        if (placeBetButton != null)
            placeBetButton.interactable = false;
    }

    private void OnEnable()
    {
        // Opening the table starts a new visit.
        sessionActive = false;
        bettingOpen = false;
        roundRunning = false;
        totalBet = 0;

        selectedBets.Clear();
        roundBets.Clear();

        if (startSicBoButton != null)
            startSicBoButton.SetActive(true);

        if (placeBetButton != null)
            placeBetButton.interactable = false;

        HideDice();

        if (diceRollAnimationObject != null)
            diceRollAnimationObject.SetActive(false);

        if (selectedBetText != null)
            selectedBetText.text = "Cửa cược: Chưa chọn";

        SetDialogue("Nhấn Bắt đầu để chơi SicBo.");
        UpdateUI();
    }

    private void OnDestroy()
    {
        if (placeBetButton != null)
            placeBetButton.onClick.RemoveListener(ConfirmBet);

        if (leaveSicBoButton != null)
            leaveSicBoButton.onClick.RemoveListener(LeaveSicBo);
    }

    // Call this once when entering the table.
    public void StartPlaying()
    {
        if (sessionActive)
            return;

        sessionActive = true;
        bettingOpen = true;
        roundRunning = false;

        if (startSicBoButton != null)
            startSicBoButton.SetActive(false);

        ClearCurrentSelection();
        SetDialogue("Chọn một hoặc nhiều cửa cược, rồi nhấn Đặt cược.");
        UpdateUI();
    }

    // Called by every SicBoBetButton.
    // Clicking a door adds it; clicking it again removes it.
    public void SelectBet(string betType, int betValue)
    {
        if (!sessionActive || !bettingOpen || roundRunning)
            return;

        string type = NormalizeBetType(betType);

        if (string.IsNullOrEmpty(type))
        {
            Debug.LogWarning("Bet Type không hợp lệ: " + betType);
            return;
        }

        if (selectedBets.Contains(type))
            selectedBets.Remove(type);
        else
            selectedBets.Add(type);

        UpdateSelectedBetText();
        UpdatePlaceBetButton();
        UpdateUI();
    }

    private string NormalizeBetType(string betType)
    {
        if (string.IsNullOrWhiteSpace(betType))
            return "";

        string value = betType.Trim().ToLowerInvariant();

        switch (value)
        {
            case "big": return "Big";
            case "small": return "Small";
            case "even": return "Even";
            case "odd": return "Odd";
            case "triple": return "Triple";
            case "triple1": return "Triple1";
            case "triple2": return "Triple2";
            case "triple3": return "Triple3";
            case "triple4": return "Triple4";
            case "triple5": return "Triple5";
            case "triple6": return "Triple6";
        }

        if (int.TryParse(value, out int sum) && sum >= 3 && sum <= 18)
            return sum.ToString();

        return "";
    }

    public void ConfirmBet()
    {
        if (!sessionActive || !bettingOpen || roundRunning)
            return;

        if (selectedBets.Count == 0)
        {
            SetDialogue("Hãy chọn ít nhất một cửa cược!");
            return;
        }

        if (betAmountInput == null ||
            !int.TryParse(betAmountInput.text, out int amount) ||
            amount <= 0)
        {
            SetDialogue("Hãy nhập số vàng cược hợp lệ!");
            return;
        }

        if (GoldManager.Instance == null)
        {
            SetDialogue("Không tìm thấy GoldManager!");
            Debug.LogError("GoldManager.Instance đang null.");
            return;
        }

        int requiredGold = amount * selectedBets.Count;

        if (GoldManager.Instance.Gold < requiredGold)
        {
            SetDialogue(
                "Không đủ vàng! Cần " + requiredGold +
                " vàng cho " + selectedBets.Count + " cửa."
            );
            return;
        }

        // Call SpendGold only after validating the full cost.
        // This works whether SpendGold returns void or bool.
        GoldManager.Instance.SpendGold(requiredGold);

        roundBets.Clear();

        foreach (string type in selectedBets)
            roundBets[type] = amount;

        totalBet = requiredGold;
        bettingOpen = false;
        roundRunning = true;

        if (placeBetButton != null)
            placeBetButton.interactable = false;

        if (betAmountInput != null)
            betAmountInput.text = "";

        SetDialogue("Đã đặt " + totalBet + " vàng. Nhà cái đang lắc xúc xắc...");
        UpdateUI();

        roundCoroutine = StartCoroutine(RollSequence());
    }

    private IEnumerator RollSequence()
    {
        HideDice();

        if (diceRollAnimationObject != null)
            diceRollAnimationObject.SetActive(false);

        if (countdownText != null)
            countdownText.text = "";

        yield return new WaitForSeconds(delayBeforeAnimation);

        if (diceRollAnimationObject != null)
            diceRollAnimationObject.SetActive(true);

        if (diceRollAnimator != null)
        {
            diceRollAnimator.Play(rollAnimationState, 0, 0f);
            diceRollAnimator.Update(0f);

            float timer = 0f;

            while (timer < animationTimeout)
            {
                AnimatorStateInfo state =
                    diceRollAnimator.GetCurrentAnimatorStateInfo(0);

                if (state.IsName(rollAnimationState) &&
                    state.normalizedTime >= 1f)
                    break;

                timer += Time.deltaTime;
                yield return null;
            }
        }
        else
        {
            yield return new WaitForSeconds(2f);
        }

        if (diceRollAnimationObject != null)
            diceRollAnimationObject.SetActive(false);

        int die1 = Random.Range(1, 7);
        int die2 = Random.Range(1, 7);
        int die3 = Random.Range(1, 7);

        int sum = die1 + die2 + die3;
        bool triple = die1 == die2 && die2 == die3;

        ShowDice(die1, die2, die3);

        int payout = CalculatePayout(die1, die2, die3, sum, triple);

        if (payout > 0 && GoldManager.Instance != null)
            GoldManager.Instance.AddGold(payout);

        SetDialogue(
            "Kết quả: " + die1 + " + " + die2 + " + " + die3 +
            " = " + sum + ". " +
            (payout > 0
                ? "Tổng tiền trả thưởng: " + payout + " vàng."
                : "Các cửa cược không trúng.")
        );

        UpdateUI();

        yield return new WaitForSeconds(diceVisibleDuration);

        HideDice();

        // Automatically open betting for the next round.
        // The player does not need to press Start again.
        roundBets.Clear();
        selectedBets.Clear();
        totalBet = 0;
        roundRunning = false;
        bettingOpen = true;
        roundCoroutine = null;

        if (selectedBetText != null)
            selectedBetText.text = "Cửa cược: Chưa chọn";

        if (startSicBoButton != null)
            startSicBoButton.SetActive(false);

        if (placeBetButton != null)
            placeBetButton.interactable = false;

        SetDialogue("Ván mới! Chọn các cửa cược rồi nhấn Đặt cược.");
        UpdateUI();
    }

    private int CalculatePayout(
        int die1, int die2, int die3, int sum, bool triple)
    {
        int payout = 0;

        foreach (KeyValuePair<string, int> bet in roundBets)
        {
            string type = bet.Key;
            int amount = bet.Value;
            bool won = false;
            int multiplier = 2;

            if (type == "Big")
                won = !triple && sum >= 11 && sum <= 17;
            else if (type == "Small")
                won = !triple && sum >= 4 && sum <= 10;
            else if (type == "Even")
                won = !triple && sum % 2 == 0;
            else if (type == "Odd")
                won = !triple && sum % 2 != 0;
            else if (type == "Triple")
            {
                won = triple;
                multiplier = tripleNumberMultiplier;
            }
            else if (type.StartsWith("Triple"))
            {
                if (int.TryParse(type.Substring(6), out int face))
                {
                    won = die1 == face && die2 == face && die3 == face;
                    multiplier = tripleNumberMultiplier;
                }
            }
            else if (int.TryParse(type, out int targetSum))
            {
                won = sum == targetSum;
                multiplier = GetSumMultiplier(targetSum);
            }

            if (won)
                payout += amount * multiplier;
        }

        return payout;
    }

    private int GetSumMultiplier(int sum)
    {
        switch (sum)
        {
            case 3:
            case 18: return 30;
            case 4:
            case 17: return 20;
            case 5:
            case 16: return 15;
            case 6:
            case 15: return 10;
            case 7:
            case 14: return 8;
            case 8:
            case 13: return 6;
            case 9:
            case 12: return 5;
            case 10:
            case 11: return 4;
            default: return 2;
        }
    }

    private void ClearCurrentSelection()
    {
        selectedBets.Clear();

        if (selectedBetText != null)
            selectedBetText.text = "Cửa cược: Chưa chọn";

        UpdatePlaceBetButton();
    }

    private void UpdateSelectedBetText()
    {
        if (selectedBetText == null)
            return;

        if (selectedBets.Count == 0)
        {
            selectedBetText.text = "Cửa cược: Chưa chọn";
            return;
        }

        List<string> sorted = selectedBets
            .OrderBy(x => int.TryParse(x, out int n) ? n : 100)
            .ThenBy(x => x)
            .ToList();

        selectedBetText.text = "Cửa cược: " + string.Join(", ", sorted);
    }

    private void UpdatePlaceBetButton()
    {
        if (placeBetButton != null)
        {
            placeBetButton.interactable =
                sessionActive && bettingOpen &&
                !roundRunning && selectedBets.Count > 0;
        }
    }

    private void ShowDice(int die1, int die2, int die3)
    {
        SetDiceSprite(resultDice1, die1);
        SetDiceSprite(resultDice2, die2);
        SetDiceSprite(resultDice3, die3);

        if (resultDice1 != null) resultDice1.gameObject.SetActive(true);
        if (resultDice2 != null) resultDice2.gameObject.SetActive(true);
        if (resultDice3 != null) resultDice3.gameObject.SetActive(true);
    }

    private void SetDiceSprite(Image target, int value)
    {
        if (target == null)
            return;

        switch (value)
        {
            case 1: target.sprite = diceFace1; break;
            case 2: target.sprite = diceFace2; break;
            case 3: target.sprite = diceFace3; break;
            case 4: target.sprite = diceFace4; break;
            case 5: target.sprite = diceFace5; break;
            case 6: target.sprite = diceFace6; break;
        }
    }

    private void HideDice()
    {
        if (resultDice1 != null) resultDice1.gameObject.SetActive(false);
        if (resultDice2 != null) resultDice2.gameObject.SetActive(false);
        if (resultDice3 != null) resultDice3.gameObject.SetActive(false);
    }

    private void UpdateUI()
    {
        if (totalBetText != null)
            totalBetText.text = "Tổng cược: " + totalBet;

        if (goldText != null && GoldManager.Instance != null)
            goldText.text = "Vàng: " + GoldManager.Instance.Gold;

        UpdateSelectedBetText();
        UpdatePlaceBetButton();
    }

    private void SetDialogue(string message)
    {
        if (dealerDialogueText != null)
            dealerDialogueText.text = message;
    }

    public void LeaveSicBo()
    {
        if (roundRunning)
        {
            SetDialogue("Hãy đợi ván cược kết thúc rồi rời bàn.");
            return;
        }

        sessionActive = false;
        bettingOpen = false;

        if (sicBoCanvas != null)
            sicBoCanvas.SetActive(false);
    }
}
