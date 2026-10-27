using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BaccaratGameManager : MonoBehaviour
{
    [Header("Baccarat UI")]
    [SerializeField] private GameObject baccaratPanel;
    [SerializeField] private GameObject dealerDialogueImage;
    [SerializeField] private TMP_Text dealerDialogueText;

    [Header("Card Library")]
    [SerializeField] private ScriptableObject cardLibrary;

    [Header("Bet UI")]
    [SerializeField] private TMP_InputField betAmountInput;
    [SerializeField] private TMP_Text goldText;

    [Header("Score UI")]
    [SerializeField] private TMP_Text playerScoreText;
    [SerializeField] private TMP_Text bankerScoreText;

    [Header("Player Cards")]
    [SerializeField] private Image playerCard1;
    [SerializeField] private Image playerCard2;
    [SerializeField] private Image playerCard3;

    [Header("Banker Cards")]
    [SerializeField] private Image bankerCard1;
    [SerializeField] private Image bankerCard2;
    [SerializeField] private Image bankerCard3;

    [Header("Buttons")]
    [SerializeField] private Button betPlayerButton;
    [SerializeField] private Button betTieButton;
    [SerializeField] private Button betBankerButton;
    [SerializeField] private Button confirmBetButton;
    [SerializeField] private Button dealButton;
    [SerializeField] private Button startBaccaratButton;
    [SerializeField] private Button closeButton;

    [Header("Payout Rules")]
    [SerializeField, Min(0)] private float bankerCommission = 0.05f;
    [SerializeField, Min(1)] private int tieMultiplier = 8;
    [SerializeField, Min(0)] private float dealDelay = 1.2f;

    private readonly List<Sprite> faceCards = new List<Sprite>();
    private readonly List<int> deck = new List<int>();

    private int playerBet;
    private int tieBet;
    private int bankerBet;

    private bool selectPlayer;
    private bool selectTie;
    private bool selectBanker;

    private bool betConfirmed;
    private bool roundRunning;
    private bool roundFinished;
    // Start is clicked once per visit; it stays hidden between rounds.
    private bool hasStarted;

    private int playerScore;
    private int bankerScore;

    private Image[] playerCardImages;
    private Image[] bankerCardImages;

    private void Awake()
    {
        playerCardImages = new Image[]
        {
            playerCard1, playerCard2, playerCard3
        };

        bankerCardImages = new Image[]
        {
            bankerCard1, bankerCard2, bankerCard3
        };

        FindUIReferences();
        RegisterButtons();

        LoadCardSprites();
        ClearCards();
        UpdateGoldDisplay();

        // Không tự tắt BaccaratPanel trong Awake.
        // Tránh xung đột với BaccaratTableOpener.
        HideDealerDialogue();
    }

    private void FindUIReferences()
    {
        if (baccaratPanel == null)
            baccaratPanel = FindChildObject("BaccaratPanel");

        if (dealerDialogueImage == null)
            dealerDialogueImage = FindChildObject("DealerDialogueImage");

        if (dealerDialogueText == null && dealerDialogueImage != null)
        {
            dealerDialogueText =
                dealerDialogueImage.GetComponentInChildren<TMP_Text>(true);
        }
    }

    private GameObject FindChildObject(string objectName)
    {
        Transform[] objects =
            Resources.FindObjectsOfTypeAll<Transform>();

        foreach (Transform item in objects)
        {
            if (item.name == objectName &&
                item.gameObject.scene == gameObject.scene)
            {
                return item.gameObject;
            }
        }

        return null;
    }

    private void RegisterButtons()
    {
        if (betPlayerButton != null)
        {
            betPlayerButton.onClick.RemoveListener(SelectPlayerBet);
            betPlayerButton.onClick.AddListener(SelectPlayerBet);
        }

        if (betTieButton != null)
        {
            betTieButton.onClick.RemoveListener(SelectTieBet);
            betTieButton.onClick.AddListener(SelectTieBet);
        }

        if (betBankerButton != null)
        {
            betBankerButton.onClick.RemoveListener(SelectBankerBet);
            betBankerButton.onClick.AddListener(SelectBankerBet);
        }

        if (confirmBetButton != null)
        {
            confirmBetButton.onClick.RemoveListener(ConfirmBet);
            confirmBetButton.onClick.AddListener(ConfirmBet);
        }

        if (dealButton != null)
        {
            dealButton.onClick.RemoveListener(DealCards);
            dealButton.onClick.AddListener(DealCards);
        }

        if (startBaccaratButton != null)
        {
            startBaccaratButton.onClick.RemoveListener(StartBaccarat);
            startBaccaratButton.onClick.AddListener(StartBaccarat);
        }

        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(CloseBaccarat);
            closeButton.onClick.AddListener(CloseBaccarat);
        }
    }

    // =========================================================
    // OPEN / CLOSE
    // =========================================================

    public void OpenBaccarat()
    {
        if (baccaratPanel != null)
            baccaratPanel.SetActive(true);

        // When opening the table, show Start only if this visit has not started yet.
        if (startBaccaratButton != null)
            startBaccaratButton.gameObject.SetActive(!hasStarted);

        if (hasStarted)
            ShowMessage("Chọn cửa cược và số vàng để chơi ván tiếp theo.");
        else
            ShowMessage("Nhấn BẮT ĐẦU để bắt đầu chơi Baccarat.");

        UpdateGoldDisplay();
    }

    public void StartBaccarat()
    {
        if (hasStarted)
            return;

        hasStarted = true;

        if (baccaratPanel != null)
            baccaratPanel.SetActive(true);

        // Hide Start for the rest of this visit. It will return after closing the table.
        if (startBaccaratButton != null)
            startBaccaratButton.gameObject.SetActive(false);

        ResetBetSelection();
        ClearCards();

        ShowMessage("Baccarat đã bắt đầu! Hãy chọn cửa cược và số vàng.");
        UpdateGoldDisplay();
    }

    public void CloseBaccarat()
    {
        if (roundRunning)
        {
            ShowMessage("Ván bài đang diễn ra. Vui lòng đợi kết thúc.");
            return;
        }

        HideDealerDialogue();

        if (baccaratPanel != null)
            baccaratPanel.SetActive(false);

        // Closing the table ends this visit. Start will be required next time it opens.
        hasStarted = false;
        if (startBaccaratButton != null)
            startBaccaratButton.gameObject.SetActive(true);

        Debug.Log("[Baccarat] Đã đóng bàn.");
    }

    // =========================================================
    // DEALER DIALOGUE
    // =========================================================

    private void ShowMessage(string message)
    {
        if (dealerDialogueImage != null)
            dealerDialogueImage.SetActive(true);
        else
            Debug.LogError("[Baccarat] Chưa gán DealerDialogueImage!", this);

        if (dealerDialogueText != null)
        {
            dealerDialogueText.gameObject.SetActive(true);
            dealerDialogueText.text = message;
        }
        else
        {
            Debug.LogError("[Baccarat] Chưa gán DealerDialogueText!", this);
        }

        Debug.Log("[Baccarat] " + message);
    }

    private void HideDealerDialogue()
    {
        if (dealerDialogueText != null)
            dealerDialogueText.text = "";

        if (dealerDialogueImage != null)
            dealerDialogueImage.SetActive(false);
    }

    // =========================================================
    // BET SELECTION
    // =========================================================

    public void SelectPlayerBet()
    {
        if (!CanChangeBet()) return;

        selectPlayer = !selectPlayer;

        ShowMessage(selectPlayer
            ? "Bạn đã chọn cược Người chơi."
            : "Đã bỏ chọn cược Người chơi.");
    }

    public void SelectTieBet()
    {
        if (!CanChangeBet()) return;

        selectTie = !selectTie;

        ShowMessage(selectTie
            ? "Bạn đã chọn cược Hòa."
            : "Đã bỏ chọn cược Hòa.");
    }

    public void SelectBankerBet()
    {
        if (!CanChangeBet()) return;

        selectBanker = !selectBanker;

        ShowMessage(selectBanker
            ? "Bạn đã chọn cược Nhà cái."
            : "Đã bỏ chọn cược Nhà cái.");
    }

    private bool CanChangeBet()
    {
        if (!hasStarted)
        {
            ShowMessage("Hãy nhấn BẮT ĐẦU trước khi đặt cược.");
            return false;
        }

        if (roundRunning)
        {
            ShowMessage("Ván bài đang diễn ra.");
            return false;
        }

        if (betConfirmed)
        {
            ShowMessage("Bạn đã xác nhận cược. Hãy chia bài.");
            return false;
        }

        return true;
    }

    public void ConfirmBet()
    {
        if (!hasStarted)
        {
            ShowMessage("Hãy nhấn BẮT ĐẦU trước khi xác nhận cược.");
            return;
        }

        if (roundRunning)
        {
            ShowMessage("Ván bài đang diễn ra.");
            return;
        }

        if (betConfirmed)
        {
            ShowMessage("Bạn đã xác nhận cược rồi.");
            return;
        }

        if (!selectPlayer && !selectTie && !selectBanker)
        {
            ShowMessage("Hãy chọn ít nhất một cửa cược.");
            return;
        }

        if (betAmountInput == null ||
            !int.TryParse(betAmountInput.text, out int amount) ||
            amount <= 0)
        {
            ShowMessage("Vui lòng nhập số vàng cược hợp lệ.");
            return;
        }

        int total = 0;

        if (selectPlayer) total += amount;
        if (selectTie) total += amount;
        if (selectBanker) total += amount;

        if (GoldManager.Instance == null)
        {
            ShowMessage("Không tìm thấy GoldManager.");
            Debug.LogError("[Baccarat] GoldManager.Instance là null!");
            return;
        }

        if (GoldManager.Instance.Gold < total)
        {
            ShowMessage(
                "Bạn không đủ vàng! Cần " + total +
                " vàng, hiện có " + GoldManager.Instance.Gold + "."
            );
            return;
        }

        // Ghi lại số tiền cược.
        playerBet = selectPlayer ? amount : 0;
        tieBet = selectTie ? amount : 0;
        bankerBet = selectBanker ? amount : 0;

        // Trừ tổng cược đúng một lần.
        GoldManager.Instance.SpendGold(total);

        betConfirmed = true;
        roundFinished = false;

        UpdateGoldDisplay();

        ShowMessage(
            "Đã xác nhận cược " + total +
            " vàng. Nhấn CHIA BÀI để bắt đầu."
        );
    }

    private void ResetBetSelection()
    {
        playerBet = 0;
        tieBet = 0;
        bankerBet = 0;

        selectPlayer = false;
        selectTie = false;
        selectBanker = false;

        betConfirmed = false;
        roundFinished = false;
    }

    // =========================================================
    // CARD LIBRARY
    // =========================================================

    private void LoadCardSprites()
    {
        faceCards.Clear();

        if (cardLibrary == null)
        {
            Debug.LogError(
                "[Baccarat] Chưa gán Card Library trong Inspector!",
                this
            );
            return;
        }

        // Search for a Sprite[] or List<Sprite> inside the asset.
        Type type = cardLibrary.GetType();

        FieldInfo[] fields = type.GetFields(
            BindingFlags.Instance |
            BindingFlags.Public |
            BindingFlags.NonPublic
        );

        foreach (FieldInfo field in fields)
        {
            object value = field.GetValue(cardLibrary);

            if (value is Sprite[] spriteArray &&
                spriteArray.Length >= 52)
            {
                faceCards.AddRange(spriteArray);
                break;
            }

            if (value is List<Sprite> spriteList &&
                spriteList.Count >= 52)
            {
                faceCards.AddRange(spriteList);
                break;
            }
        }

        // Fallback: inspect public properties.
        if (faceCards.Count < 52)
        {
            PropertyInfo[] properties = type.GetProperties(
                BindingFlags.Instance | BindingFlags.Public
            );

            foreach (PropertyInfo property in properties)
            {
                if (!property.CanRead ||
                    property.GetIndexParameters().Length > 0)
                    continue;

                object value;

                try
                {
                    value = property.GetValue(cardLibrary);
                }
                catch
                {
                    continue;
                }

                if (value is Sprite[] spriteArray &&
                    spriteArray.Length >= 52)
                {
                    faceCards.Clear();
                    faceCards.AddRange(spriteArray);
                    break;
                }

                if (value is List<Sprite> spriteList &&
                    spriteList.Count >= 52)
                {
                    faceCards.Clear();
                    faceCards.AddRange(spriteList);
                    break;
                }
            }
        }

        if (faceCards.Count < 52)
        {
            faceCards.Clear();

            Debug.LogError(
                "[Baccarat] Không tìm thấy danh sách 52 Sprite trong Card Library. " +
                "Cần kiểm tra cấu trúc BlackjackCardLibraryAsset.",
                this
            );
        }
        else
        {
            Debug.Log(
                "[Baccarat] Đã đọc " + faceCards.Count + " lá bài."
            );
        }
    }

    // =========================================================
    // DEAL CARDS
    // =========================================================

    public void DealCards()
    {
        if (!hasStarted)
        {
            ShowMessage("Hãy nhấn BẮT ĐẦU trước khi chia bài.");
            return;
        }

        if (roundRunning)
        {
            ShowMessage("Đang chia bài. Vui lòng chờ.");
            return;
        }

        if (!betConfirmed)
        {
            ShowMessage("Hãy chọn cửa cược và xác nhận cược trước.");
            return;
        }

        if (faceCards.Count < 52)
        {
            LoadCardSprites();

            if (faceCards.Count < 52)
            {
                ShowMessage("Không thể chia bài: thư viện bài chưa hợp lệ.");
                return;
            }
        }

        StartCoroutine(DealRoutine());
    }

    private IEnumerator DealRoutine()
    {
        roundRunning = true;
        roundFinished = false;

        ClearCards();
        CreateShuffledDeck();

        ShowMessage("Nhà cái đang chia bài...");

        int p1 = DrawCard();
        int b1 = DrawCard();
        int p2 = DrawCard();
        int b2 = DrawCard();

        SetCard(playerCard1, p1);
        SetCard(bankerCard1, b1);

        yield return new WaitForSeconds(dealDelay);

        SetCard(playerCard2, p2);
        SetCard(bankerCard2, b2);

        int playerTotal = CardPoint(p1) + CardPoint(p2);
        int bankerTotal = CardPoint(b1) + CardPoint(b2);

        playerScore = playerTotal % 10;
        bankerScore = bankerTotal % 10;

        UpdateScores();

        yield return new WaitForSeconds(dealDelay);

        int p3 = -1;
        int b3 = -1;

        bool natural =
            playerScore >= 8 || bankerScore >= 8;

        if (!natural)
        {
            bool playerDraws = playerScore <= 5;

            if (playerDraws)
            {
                p3 = DrawCard();
                SetCard(playerCard3, p3);

                playerScore =
                    (playerScore + CardPoint(p3)) % 10;

                UpdateScores();

                yield return new WaitForSeconds(dealDelay);
            }

            bool bankerDraws;

            if (!playerDraws)
            {
                bankerDraws = bankerScore <= 5;
            }
            else
            {
                int thirdPoint = CardPoint(p3);

                if (bankerScore <= 2)
                    bankerDraws = true;
                else if (bankerScore == 3)
                    bankerDraws = thirdPoint != 8;
                else if (bankerScore == 4)
                    bankerDraws = thirdPoint >= 2 && thirdPoint <= 7;
                else if (bankerScore == 5)
                    bankerDraws = thirdPoint >= 4 && thirdPoint <= 7;
                else if (bankerScore == 6)
                    bankerDraws = thirdPoint == 6 || thirdPoint == 7;
                else
                    bankerDraws = false;
            }

            if (bankerDraws)
            {
                b3 = DrawCard();
                SetCard(bankerCard3, b3);

                bankerScore =
                    (bankerScore + CardPoint(b3)) % 10;

                UpdateScores();

                yield return new WaitForSeconds(dealDelay);
            }
        }

        SetCardResult(playerScore, bankerScore);

        roundRunning = false;
        roundFinished = true;

        SettleBets(playerScore, bankerScore);
    }

    private void CreateShuffledDeck()
    {
        deck.Clear();

        for (int i = 0; i < 52; i++)
            deck.Add(i);

        for (int i = deck.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);

            int temp = deck[i];
            deck[i] = deck[j];
            deck[j] = temp;
        }
    }

    private int DrawCard()
    {
        if (deck.Count == 0)
            return UnityEngine.Random.Range(0, 52);

        int lastIndex = deck.Count - 1;
        int card = deck[lastIndex];

        deck.RemoveAt(lastIndex);

        return card;
    }

    private int CardPoint(int cardIndex)
    {
        // Assumes ranks A, 2, 3, ..., 10, J, Q, K
        // repeat for each suit.
        int rank = cardIndex % 13;

        if (rank == 0)
            return 1;

        if (rank >= 10)
            return 0;

        return rank + 1;
    }

    private void SetCard(Image target, int cardIndex)
    {
        if (target == null)
            return;

        target.gameObject.SetActive(true);

        if (cardIndex >= 0 && cardIndex < faceCards.Count)
        {
            target.sprite = faceCards[cardIndex];
            target.enabled = true;
        }
    }

    private void ClearCards()
    {
        ClearImage(playerCard1);
        ClearImage(playerCard2);
        ClearImage(playerCard3);

        ClearImage(bankerCard1);
        ClearImage(bankerCard2);
        ClearImage(bankerCard3);

        if (playerScoreText != null)
            playerScoreText.text = "Người chơi: 0";

        if (bankerScoreText != null)
            bankerScoreText.text = "Nhà cái: 0";
    }

    private void ClearImage(Image image)
    {
        if (image == null)
            return;

        image.sprite = null;
        image.enabled = false;
    }

    private void UpdateScores()
    {
        if (playerScoreText != null)
            playerScoreText.text = "Người chơi: " + playerScore;

        if (bankerScoreText != null)
            bankerScoreText.text = "Nhà cái: " + bankerScore;
    }

    private void SetCardResult(int player, int banker)
    {
        playerScore = player;
        bankerScore = banker;
        UpdateScores();
    }

    // =========================================================
    // PAYOUT
    // =========================================================

    private void SettleBets(int player, int banker)
    {
        if (GoldManager.Instance == null)
        {
            ShowMessage("Không tìm thấy hệ thống vàng.");
            return;
        }

        // Stakes were already deducted at confirmation.
        long payout = 0;
        string result;

        if (player > banker)
        {
            result = "NGƯỜI CHƠI THẮNG!";

            // Return stake + 1:1 winnings.
            payout += (long)playerBet * 2;
        }
        else if (banker > player)
        {
            result = "NHÀ CÁI THẮNG!";

            // Return stake + winnings minus commission.
            payout += bankerBet;
            payout += (long)Math.Floor(
                bankerBet * (1.0 - bankerCommission)
            );
        }
        else
        {
            result = "HÒA!";

            // Tie pushes Player and Banker bets.
            payout += playerBet;
            payout += bankerBet;

            // Tie wins at 8:1, plus returned stake.
            payout += (long)tieBet * (tieMultiplier + 1);
        }

        if (payout > 0)
        {
            if (payout > int.MaxValue)
                payout = int.MaxValue;

            GoldManager.Instance.AddGold((int)payout);
        }

        UpdateGoldDisplay();

        ShowMessage(
            result +
            "\nĐiểm người chơi: " + player +
            "\nĐiểm nhà cái: " + banker +
            "\nTiền nhận lại: " + payout +
            " vàng."
        );

        Debug.Log(
            "[Baccarat] Kết quả: " + result +
            " | Player: " + player +
            " | Banker: " + banker +
            " | Payout: " + payout
        );

        // Allow a new round.
        ResetBetSelection();
    }

    private void UpdateGoldDisplay()
    {
        if (goldText != null && GoldManager.Instance != null)
        {
            goldText.text =
                "Vàng: " + GoldManager.Instance.Gold;
        }
    }
}

