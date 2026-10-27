
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BlackjackGameManager : MonoBehaviour
{
    [Header("Card Library")]
    [SerializeField] private BlackjackCardLibrary cardLibrary;

    [Header("Player Cards - 10 slots")]
    [SerializeField]
    private BlackjackCardSlot[] playerCards =
        new BlackjackCardSlot[10];

    [Header("Dealer Cards - 10 slots")]
    [SerializeField]
    private BlackjackCardSlot[] dealerCards =
        new BlackjackCardSlot[10];

    [Header("Bet UI")]
    [SerializeField] private TMP_InputField betAmountInput;
    [SerializeField] private Button confirmBetButton;
    [SerializeField] private TMP_Text confirmedBetText;
    [SerializeField] private int defaultBet = 100;

    [Header("Display Text")]
    [SerializeField] private TMP_Text blackjackScoreText;
    [SerializeField] private TMP_Text blackjackGoldText;
    [SerializeField] private TMP_Text dealerDialogueText;
    [SerializeField] private TMP_Text playerDialogueText;

    [Header("Buttons")]
    [SerializeField] private Button startButton;
    [Tooltip("Gan TMP_Text nay neu chu BAT DAU nam ngoai GameObject cua StartButton.")]
    [SerializeField] private TMP_Text startButtonLabel;
    [SerializeField] private Button hitButton;
    [SerializeField] private Button standButton;
    [SerializeField] private Button doubleButton;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button exitButton;

    [Header("Dealer Animation")]
    [SerializeField] private Animator dealerAnimator;
    [SerializeField] private string drawTrigger = "DrawCard";
    [SerializeField] private float drawAnimationDuration = 0.7f;
    [SerializeField] private float delayBeforeCardAppears = 0.5f;

    private readonly List<int> deck = new List<int>();
    private readonly List<int> playerHand = new List<int>();
    private readonly List<int> dealerHand = new List<int>();

    private int playerCardCount;
    private int dealerCardCount;
    private int currentBet;
    private int confirmedBetAmount;

    private bool betConfirmed;
    private bool roundActive;
    private bool busy;
    private bool doubled;
    private bool dealerSecondCardHidden;

    private void Awake()
    {
        // Connect buttons in code.
        // Do not add the same OnClick events again in Inspector.
        if (confirmBetButton != null)
            confirmBetButton.onClick.AddListener(ConfirmBet);

        if (startButton != null)
            startButton.onClick.AddListener(StartRound);

        if (hitButton != null)
            hitButton.onClick.AddListener(Hit);

        if (standButton != null)
            standButton.onClick.AddListener(Stand);

        if (doubleButton != null)
            doubleButton.onClick.AddListener(DoubleDown);

        if (restartButton != null)
            restartButton.onClick.AddListener(PrepareNewRound);

        if (exitButton != null)
            exitButton.onClick.AddListener(ExitBlackjack);

        if (betAmountInput != null)
        {
            betAmountInput.contentType =
                TMP_InputField.ContentType.IntegerNumber;

            betAmountInput.SetTextWithoutNotify(
                defaultBet.ToString());
        }

        ClearAllCards();
        SetTableButtons(false);

        SetVisible(startButton, true);
        SetVisible(startButtonLabel, true);
        SetVisible(restartButton, false);

        SetBetControls(true);

        if (startButton != null)
            startButton.interactable = false;

        if (restartButton != null)
            restartButton.interactable = false;

        UpdateGoldText();
        UpdateScoreText();

        SetText(confirmedBetText, "Mức cược: Chưa xác nhận");
        SetText(dealerDialogueText, "Nhà cái: Chào mừng bạn!");
        SetText(playerDialogueText,
            "Nhập mức cược, xác nhận rồi nhấn BẮT ĐẦU.");
    }

    private void OnDestroy()
    {
        if (confirmBetButton != null)
            confirmBetButton.onClick.RemoveListener(ConfirmBet);

        if (startButton != null)
            startButton.onClick.RemoveListener(StartRound);

        if (hitButton != null)
            hitButton.onClick.RemoveListener(Hit);

        if (standButton != null)
            standButton.onClick.RemoveListener(Stand);

        if (doubleButton != null)
            doubleButton.onClick.RemoveListener(DoubleDown);

        if (restartButton != null)
            restartButton.onClick.RemoveListener(PrepareNewRound);

        if (exitButton != null)
            exitButton.onClick.RemoveListener(ExitBlackjack);
    }

    // --------------------------------------------------
    // BET
    // --------------------------------------------------

    public void ConfirmBet()
    {
        if (busy || roundActive)
            return;

        if (GoldManager.Instance == null)
        {
            SetText(playerDialogueText,
                "Lỗi: Không tìm thấy GoldManager.");
            Debug.LogError("Blackjack: GoldManager.Instance is null.");
            return;
        }

        if (betAmountInput == null)
        {
            SetText(playerDialogueText,
                "Lỗi: Chưa gán BetAmountInput.");
            Debug.LogError("Blackjack: Bet Amount Input is not assigned.");
            return;
        }

        int amount;

        if (!int.TryParse(betAmountInput.text, out amount) ||
            amount <= 0)
        {
            betConfirmed = false;
            SetText(confirmedBetText, "Mức cược không hợp lệ");
            SetText(playerDialogueText,
                "Hãy nhập số vàng lớn hơn 0.");
            UpdateStartButtons();
            return;
        }

        if (amount > GoldManager.Instance.Gold)
        {
            betConfirmed = false;
            SetText(confirmedBetText, "Không đủ vàng");
            SetText(playerDialogueText,
                "Bạn không đủ vàng để cược mức này.");
            UpdateStartButtons();
            return;
        }

        confirmedBetAmount = amount;
        betConfirmed = true;

        SetText(confirmedBetText,
            "Mức cược: " + amount + " vàng");

        SetText(playerDialogueText,
            "Đã xác nhận cược. Nhấn BẮT ĐẦU.");

        UpdateGoldText();
        UpdateStartButtons();
    }

    public void PrepareNewRound()
    {
        if (roundActive || busy)
            return;

        // A new round must begin with a fresh bet confirmation.
        betConfirmed = false;
        confirmedBetAmount = 0;
        currentBet = 0;
        doubled = false;

        SetVisible(restartButton, false);
        SetVisible(startButton, true);
        SetVisible(startButtonLabel, true);
        SetVisible(doubleButton, false);
        SetTableButtons(false);
        SetBetControls(true);

        if (betAmountInput != null)
            betAmountInput.SetTextWithoutNotify(defaultBet.ToString());

        if (startButton != null)
            startButton.interactable = false;

        SetText(confirmedBetText, "Mức cược: Chưa xác nhận");
        SetText(playerDialogueText,
            "Nhập mức cược, nhấn XÁC NHẬN rồi nhấn BẮT ĐẦU.");

        UpdateGoldText();
        UpdateScoreText();
    }

    private void UpdateStartButtons()
    {
        bool canStart = betConfirmed && !roundActive && !busy;

        if (startButton != null && startButton.gameObject.activeSelf)
            startButton.interactable = canStart;

        if (restartButton != null && restartButton.gameObject.activeSelf)
            restartButton.interactable = canStart;
    }

    // --------------------------------------------------
    // START
    // --------------------------------------------------

    public void StartRound()
    {
        Debug.Log("Blackjack: StartRound button clicked.");

        if (busy || roundActive)
        {
            Debug.LogWarning("Blackjack: A round is already running.");
            return;
        }

        if (!betConfirmed)
        {
            SetText(playerDialogueText,
                "Hãy nhập và xác nhận mức cược trước.");
            Debug.LogWarning("Blackjack: Bet has not been confirmed.");
            return;
        }

        if (!ValidateReferences())
        {
            SetText(playerDialogueText,
                "Thiếu tham chiếu. Kiểm tra Console.");
            return;
        }

        if (GoldManager.Instance == null)
        {
            SetText(playerDialogueText,
                "Lỗi: Không tìm thấy GoldManager.");
            return;
        }

        if (confirmedBetAmount <= 0 ||
            GoldManager.Instance.Gold < confirmedBetAmount)
        {
            betConfirmed = false;
            SetText(playerDialogueText,
                "Không đủ vàng. Hãy xác nhận cược lại.");
            UpdateGoldText();
            UpdateStartButtons();
            return;
        }

        // SpendGold is called as a statement so this works whether
        // the existing method returns void or bool.
        // The balance was checked immediately above.
        GoldManager.Instance.SpendGold(confirmedBetAmount);

        currentBet = confirmedBetAmount;
        betConfirmed = false;
        doubled = false;

        roundActive = true;
        busy = true;

        playerCardCount = 0;
        dealerCardCount = 0;

        playerHand.Clear();
        dealerHand.Clear();

        ClearAllCards();
        CreateDeck();
        ShuffleDeck();

        SetVisible(startButton, false);
        SetVisible(startButtonLabel, false);
        SetVisible(restartButton, false);
        SetTableButtons(false);
        SetBetControls(false);

        UpdateGoldText();
        UpdateScoreText();

        SetText(dealerDialogueText, "Nhà cái: Bắt đầu chia bài.");
        SetText(playerDialogueText, "Đang chia bài...");

        StartCoroutine(InitialDeal());
    }

    private IEnumerator InitialDeal()
    {
        yield return DealCard(false, true);
        yield return DealCard(true, true);
        yield return DealCard(false, true);
        yield return DealCard(true, false);

        dealerSecondCardHidden = true;

        UpdateScoreText();
        UpdateGoldText();

        int playerScore = GetHandValue(playerHand);
        int dealerScore = GetHandValue(dealerHand);

        bool playerNatural = playerHand.Count == 2 && playerScore == 21;
        bool dealerNatural = dealerHand.Count == 2 && dealerScore == 21;

        if (playerNatural || dealerNatural)
        {
            yield return DealerTurn();
            yield break;
        }

        busy = false;
        SetTableButtons(true);
        SetVisible(doubleButton, true);

        SetText(playerDialogueText,
            "Bạn có " + playerScore +
            " điểm. Rút thêm hay dừng?");
    }

    // --------------------------------------------------
    // PLAYER ACTIONS
    // --------------------------------------------------

    public void Hit()
    {
        if (!roundActive || busy)
            return;

        if (playerCardCount >= playerCards.Length)
        {
            SetText(playerDialogueText, "Không còn ô bài trống.");
            return;
        }

        StartCoroutine(PlayerHitRoutine());
    }

    private IEnumerator PlayerHitRoutine()
    {
        busy = true;
        SetTableButtons(false);

        yield return DealCard(false, true);

        int score = GetHandValue(playerHand);
        UpdateScoreText();

        if (score > 21)
        {
            SetText(playerDialogueText,
                "Bạn quắc với " + score + " điểm. Bạn thua.");
            FinishRound("BUST");
            yield break;
        }

        if (score == 21)
        {
            yield return DealerTurn();
            yield break;
        }

        busy = false;
        SetTableButtons(true);
        SetVisible(doubleButton, false);

        SetText(playerDialogueText,
            "Bạn có " + score + " điểm. Rút thêm hay dừng?");
    }

    public void Stand()
    {
        if (!roundActive || busy)
            return;

        StartCoroutine(DealerTurn());
    }

    public void DoubleDown()
    {
        if (!roundActive || busy ||
            playerHand.Count != 2 || doubled)
            return;

        if (GoldManager.Instance == null ||
            GoldManager.Instance.Gold < currentBet)
        {
            SetText(playerDialogueText,
                "Bạn không đủ vàng để gấp đôi cược.");
            return;
        }

        GoldManager.Instance.SpendGold(currentBet);
        currentBet *= 2;
        doubled = true;

        UpdateGoldText();
        StartCoroutine(DoubleDownRoutine());
    }

    private IEnumerator DoubleDownRoutine()
    {
        busy = true;
        SetTableButtons(false);

        yield return DealCard(false, true);

        int score = GetHandValue(playerHand);
        UpdateScoreText();

        if (score > 21)
        {
            SetText(playerDialogueText, "Bạn quắc. Bạn thua.");
            FinishRound("BUST");
            yield break;
        }

        yield return DealerTurn();
    }

    // --------------------------------------------------
    // DEALER
    // --------------------------------------------------

    private IEnumerator DealerTurn()
    {
        if (!roundActive)
            yield break;

        busy = true;
        SetTableButtons(false);
        SetVisible(doubleButton, false);

        if (dealerSecondCardHidden && dealerCardCount >= 2)
        {
            dealerCards[1].FlipFaceUp();
            dealerSecondCardHidden = false;

            UpdateScoreText();
            SetText(dealerDialogueText, "Nhà cái lật lá bài úp.");

            yield return new WaitForSeconds(0.4f);
        }

        // Dealer stands on all 17s, including soft 17.
        while (GetHandValue(dealerHand) < 17 &&
               dealerCardCount < dealerCards.Length)
        {
            SetText(dealerDialogueText, "Nhà cái rút thêm bài...");
            yield return DealCard(true, true);
        }

        int dealerScore = GetHandValue(dealerHand);
        int playerScore = GetHandValue(playerHand);

        SetText(dealerDialogueText,
            "Nhà cái có " + dealerScore + " điểm.");

        yield return new WaitForSeconds(0.3f);

        // Natural Blackjack pays 3:2 profit.
        bool playerNatural =
            playerHand.Count == 2 && playerScore == 21;

        bool dealerNatural =
            dealerHand.Count == 2 && dealerScore == 21;

        if (playerNatural && dealerNatural)
            FinishRound("PUSH");
        else if (playerNatural)
            FinishRound("BLACKJACK");
        else if (playerScore > 21)
            FinishRound("BUST");
        else if (dealerScore > 21)
            FinishRound("DEALER_BUST");
        else if (playerScore > dealerScore)
            FinishRound("WIN");
        else if (playerScore < dealerScore)
            FinishRound("LOSE");
        else
            FinishRound("PUSH");
    }

    // --------------------------------------------------
    // DEAL CARD + OPTIONAL ANIMATION
    // --------------------------------------------------

    private IEnumerator DealCard(bool toDealer, bool faceUp)
    {
        BlackjackCardSlot[] slots = toDealer ? dealerCards : playerCards;
        List<int> hand = toDealer ? dealerHand : playerHand;

        int slotIndex = toDealer ? dealerCardCount : playerCardCount;

        if (slotIndex >= slots.Length || deck.Count == 0)
        {
            Debug.LogWarning("Blackjack: No card slot or no cards left.");
            yield break;
        }

        int cardIndex = deck[0];
        deck.RemoveAt(0);

        // Animation is optional. Missing Animator won't stop dealing.
        if (dealerAnimator != null &&
            !string.IsNullOrEmpty(drawTrigger))
        {
            dealerAnimator.ResetTrigger(drawTrigger);
            dealerAnimator.SetTrigger(drawTrigger);

            if (drawAnimationDuration > 0f)
                yield return new WaitForSeconds(drawAnimationDuration);

            dealerAnimator.ResetTrigger(drawTrigger);
        }

        // Wait before displaying the card.
        if (delayBeforeCardAppears > 0f)
            yield return new WaitForSeconds(delayBeforeCardAppears);

        hand.Add(cardIndex);

        if (faceUp)
            slots[slotIndex].ShowFaceUp(cardIndex);
        else
            slots[slotIndex].ShowFaceDown(cardIndex);

        if (toDealer)
            dealerCardCount++;
        else
            playerCardCount++;

        UpdateScoreText();
        UpdateGoldText();
    }

    // --------------------------------------------------
    // RESULT + PAYOUT
    // --------------------------------------------------

    private void FinishRound(string result)
    {
        // Prevent a second settlement if multiple paths reach FinishRound.
        if (!roundActive)
            return;

        // Reveal the dealer's hidden card even when the player busts.
        if (dealerSecondCardHidden && dealerCardCount >= 2 &&
            dealerCards != null && dealerCards.Length > 1 && dealerCards[1] != null)
        {
            dealerCards[1].FlipFaceUp();
            dealerSecondCardHidden = false;
        }

        roundActive = false;
        busy = false;

        int playerScore = GetHandValue(playerHand);
        int dealerScore = GetHandValue(dealerHand);

        switch (result)
        {
            case "BUST":
                SetText(playerDialogueText,
                    "Bạn quắc với " + playerScore + " điểm. Bạn thua.");
                break;

            case "DEALER_BUST":
                if (GoldManager.Instance != null)
                    GoldManager.Instance.AddGold(currentBet * 2);
                SetText(playerDialogueText,
                    "Nhà cái quắc. Bạn thắng " + currentBet + " vàng.");
                break;

            case "WIN":
                if (GoldManager.Instance != null)
                    GoldManager.Instance.AddGold(currentBet * 2);
                SetText(playerDialogueText,
                    "Bạn thắng! " + playerScore + " - " + dealerScore + ".");
                break;

            case "LOSE":
                SetText(playerDialogueText,
                    "Bạn thua! " + playerScore + " - " + dealerScore + ".");
                break;

            case "PUSH":
                if (GoldManager.Instance != null)
                    GoldManager.Instance.AddGold(currentBet);
                SetText(playerDialogueText,
                    "Hòa! Hoàn lại " + currentBet + " vàng.");
                break;

            case "BLACKJACK":
                int payout = currentBet + (currentBet * 3 / 2);
                if (GoldManager.Instance != null)
                    GoldManager.Instance.AddGold(payout);
                SetText(playerDialogueText,
                    "Blackjack! Lãi " + (payout - currentBet) + " vàng.");
                break;
        }

        UpdateGoldText();
        UpdateScoreText();

        SetTableButtons(false);
        SetVisible(doubleButton, false);
        SetVisible(startButton, false);
        SetVisible(startButtonLabel, false);
        SetVisible(restartButton, true);

        // The player must press NEW ROUND before entering the next bet.
        SetBetControls(false);
        betConfirmed = false;
        confirmedBetAmount = 0;

        SetText(confirmedBetText, "Mức cược: Chưa xác nhận");
        if (restartButton != null)
            restartButton.interactable = true;
        UpdateGoldText();
        UpdateScoreText();
    }

    // --------------------------------------------------
    // DECK + HAND VALUE
    // --------------------------------------------------

    private void CreateDeck()
    {
        deck.Clear();

        for (int i = 0; i < 52; i++)
            deck.Add(i);
    }

    private void ShuffleDeck()
    {
        for (int i = deck.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            int temp = deck[i];
            deck[i] = deck[j];
            deck[j] = temp;
        }
    }

    private int GetHandValue(List<int> hand)
    {
        int total = 0;
        int aces = 0;

        foreach (int cardIndex in hand)
        {
            // Requires each suit to be ordered A, 2 ... 10, J, Q, K.
            int rank = cardIndex % 13;

            if (rank == 0)
            {
                total += 11;
                aces++;
            }
            else if (rank >= 10)
            {
                total += 10;
            }
            else
            {
                total += rank + 1;
            }
        }

        while (total > 21 && aces > 0)
        {
            total -= 10;
            aces--;
        }

        return total;
    }

    // --------------------------------------------------
    // UI HELPERS
    // --------------------------------------------------

    private void UpdateScoreText()
    {
        if (blackjackScoreText == null)
            return;

        int playerScore = GetHandValue(playerHand);

        if (dealerSecondCardHidden && dealerHand.Count >= 2)
        {
            int visibleScore =
                GetHandValue(new List<int> { dealerHand[0] });

            blackjackScoreText.text =
                "PLAYER: " + playerScore +
                " | DEALER: " + visibleScore + " + ?";
        }
        else
        {
            blackjackScoreText.text =
                "PLAYER: " + playerScore +
                " | DEALER: " + GetHandValue(dealerHand);
        }
    }

    private void UpdateGoldText()
    {
        if (blackjackGoldText == null)
            return;

        int gold = GoldManager.Instance != null
            ? GoldManager.Instance.Gold
            : 0;

        blackjackGoldText.text = "VÀNG: " + gold;
    }

    private void ClearAllCards()
    {
        if (playerCards != null)
        {
            foreach (BlackjackCardSlot slot in playerCards)
                if (slot != null)
                    slot.ShowEmpty();
        }

        if (dealerCards != null)
        {
            foreach (BlackjackCardSlot slot in dealerCards)
                if (slot != null)
                    slot.ShowEmpty();
        }

        dealerSecondCardHidden = false;
    }

    private void SetTableButtons(bool enabledState)
    {
        if (hitButton != null)
            hitButton.interactable = enabledState;

        if (standButton != null)
            standButton.interactable = enabledState;

        if (doubleButton != null)
        {
            doubleButton.interactable =
                enabledState && playerHand.Count == 2 && !doubled;
        }
    }

    private void SetBetControls(bool enabledState)
    {
        if (betAmountInput != null)
            betAmountInput.interactable = enabledState;

        if (confirmBetButton != null)
            confirmBetButton.interactable = enabledState;
    }

    private void SetVisible(Button button, bool visible)
    {
        if (button != null)
            button.gameObject.SetActive(visible);
    }

    private void SetVisible(TMP_Text target, bool visible)
    {
        if (target != null)
            target.gameObject.SetActive(visible);
    }

    private void SetText(TMP_Text target, string value)
    {
        if (target != null)
            target.text = value;
    }

    private bool ValidateReferences()
    {
        if (cardLibrary == null)
        {
            Debug.LogError(
                "Blackjack: Assign BlackjackCardLibraryAsset.");
            return false;
        }

        if (cardLibrary.faceCards == null ||
            cardLibrary.faceCards.Length < 52 ||
            cardLibrary.cardBack == null)
        {
            Debug.LogError(
                "Blackjack: Library needs 52 face cards and a card back.");
            return false;
        }

        if (playerCards == null || playerCards.Length < 10 ||
            dealerCards == null || dealerCards.Length < 10)
        {
            Debug.LogError(
                "Blackjack: Both card arrays must have 10 slots.");
            return false;
        }

        for (int i = 0; i < 10; i++)
        {
            if (playerCards[i] == null || dealerCards[i] == null)
            {
                Debug.LogError(
                    "Blackjack: Assign every player and dealer card slot.");
                return false;
            }
        }

        return true;
    }

    private void ExitBlackjack()
    {
        StopAllCoroutines();

        roundActive = false;
        busy = false;
        betConfirmed = false;

        gameObject.SetActive(false);
    }
}
