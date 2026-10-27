using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Texas Hold'em for 1 human + 4 NPCs.
/// Card sprite names must contain rank+suit, e.g. AS, 10H, QD, KC, 2C.
/// S=Spades/bich, H=Hearts/co, D=Diamonds/ro, C=Clubs/tep.
/// </summary>
public class PokerBettingUI : MonoBehaviour
{
    [Header("Poker Deck")]
    [SerializeField] private PokerDeck pokerDeck;

    [Header("Main Buttons")]
    [SerializeField] private Button startPokerButton;
    [SerializeField] private Button exitPokerButton;
    [SerializeField] private GameObject pokerCanvas;

    [Header("Player Cards: 2 slots")]
    [SerializeField] private Image[] playerCards = new Image[2];

    [Header("NPC Cards: 8 slots, 2 per NPC")]
    [SerializeField] private Image[] npcCards = new Image[8];

    [Header("Community Cards: 5 slots")]
    [SerializeField] private Image[] communityCards = new Image[5];

    [Header("NPC Names")]
    [SerializeField] private string[] npcNames = { "Arthur", "William", "Thomas", "Edward" };

    [Header("NPC Bankroll - randomized at session start, retained between hands")]
    [SerializeField, Min(0)] private int npcMinGold = 2500;
    [SerializeField, Min(0)] private int npcMaxGold = 50000;
    [SerializeField] private bool randomizeNpcGoldOnSceneStart = true;

    [Header("NPC Behavior")]
    [SerializeField, Range(0.1f, 10f)] private float npcMinThinkTime = 1f;
    [SerializeField, Range(0.1f, 10f)] private float npcMaxThinkTime = 10f;
    [SerializeField, Min(0f)] private float cardRevealDelay = 0.4f;
    [SerializeField, Range(0f, 1f)] private float npcFoldChanceFacingBet = 0.18f;
    [SerializeField, Range(0f, 1f)] private float npcRaiseChance = 0.22f;
    [SerializeField, Min(1)] private int npcRaiseMin = 100;
    [SerializeField, Min(1)] private int npcRaiseMax = 2500;

    [Header("Blind and betting rules")]
    [SerializeField, Min(1)] private int smallBlind = 50;
    [SerializeField, Min(1)] private int bigBlind = 100;
    [SerializeField, Min(1)] private int minimumBet = 100;
    [SerializeField, Min(1)] private int maximumRaisesPerStreet = 4;

    [Header("Chip Buttons")]
    [SerializeField] private Button chip1Button;
    [SerializeField] private Button chip5Button;
    [SerializeField] private Button chip10Button;
    [SerializeField] private Button chip50Button;
    [SerializeField] private Button chip100Button;
    [SerializeField] private Button chip500Button;
    [SerializeField] private Button chip1KButton;
    [SerializeField] private Button chip5KButton;
    [SerializeField] private Button chip10KButton;
    [SerializeField] private Button chip50KButton;

    [Header("Bet Buttons")]
    [SerializeField] private Button betButton;
    [SerializeField] private Button callButton;
    [SerializeField] private Button raiseButton;
    [SerializeField] private Button foldButton;

    [Header("UI Text")]
    [SerializeField] private TMP_Text currentBetText;
    [SerializeField] private TMP_Text goldAmountText;
    [SerializeField] private TMP_Text dealerMessageText;
    [SerializeField] private TMP_InputField raiseAmountInput;
    [SerializeField] private TMP_Text handReportText;

    private const int SeatCount = 5; // seat 0 is the player
    private readonly List<Card> deck = new List<Card>();
    private readonly Card[] playerHole = new Card[2];
    private readonly Card[][] npcHole =
    {
        new Card[2], new Card[2], new Card[2], new Card[2]
    };
    private readonly Card[] board = new Card[5];

    // All monetary values in this script are chips. GoldManager is the player's persistent wallet.
    private readonly int[] stack = new int[SeatCount];
    private readonly int[] streetContribution = new int[SeatCount];
    private readonly int[] handContribution = new int[SeatCount];
    private readonly bool[] folded = new bool[SeatCount];
    private readonly bool[] allIn = new bool[SeatCount];
    private readonly bool[] actedSinceLastRaise = new bool[SeatCount];

    private int pot;
    private int currentBet;
    private int lastFullRaise;
    private int boardCount;
    private int dealerSeat;
    private int bigBlindSeatForHand;
    private int selectedBet;
    private int raiseCount;
    private int playerGoldAtStart;
    private int playerTotalInvested;
    private int playerPayout;
    private bool handActive;
    private bool waitingForPlayer;
    private bool playerSubmittedAction;
    private ActionKind submittedAction;
    private int submittedAmount;
    private bool hasFinishedHand;
    private Street street;
    private Coroutine gameCoroutine;

    private enum Street { PreFlop, Flop, Turn, River, Showdown }
    private enum ActionKind { CheckCall, BetRaise, Fold }
    private enum HandCategory
    {
        HighCard = 1, Pair, TwoPair, ThreeOfAKind, Straight,
        Flush, FullHouse, FourOfAKind, StraightFlush, RoyalFlush
    }

    private sealed class Card
    {
        public int Rank;
        public char Suit;
        public Sprite Sprite;
        public string Name => RankText(Rank) + SuitText(Suit);
    }

    private sealed class HandValue : IComparable<HandValue>
    {
        public HandCategory Category;
        public int[] TieBreak;
        public List<Card> BestFive;

        public string CategoryName => Category switch
        {
            HandCategory.RoyalFlush => "Sảnh chúa",
            HandCategory.StraightFlush => "Thùng phá sảnh",
            HandCategory.FourOfAKind => "Tứ quý",
            HandCategory.FullHouse => "Cù lũ",
            HandCategory.Flush => "Thùng",
            HandCategory.Straight => "Sảnh",
            HandCategory.ThreeOfAKind => "Sám cô",
            HandCategory.TwoPair => "Hai đôi",
            HandCategory.Pair => "Một đôi",
            _ => "Bài cao"
        };

        public int CompareTo(HandValue other)
        {
            if (other == null) return 1;
            int result = Category.CompareTo(other.Category);
            if (result != 0) return result;
            for (int i = 0; i < Math.Min(TieBreak.Length, other.TieBreak.Length); i++)
            {
                result = TieBreak[i].CompareTo(other.TieBreak[i]);
                if (result != 0) return result;
            }
            return 0;
        }
    }

    private void Awake()
    {
        if (startPokerButton == null)
        {
            foreach (Transform child in GetComponentsInChildren<Transform>(true))
            {
                string n = child.name.ToLowerInvariant();
                if (n.Contains("startpoker") || n == "startbutton" || n == "start poker")
                {
                    startPokerButton = child.GetComponent<Button>();
                    if (startPokerButton != null) break;
                }
            }
        }
    }

    private void Start()
    {
        ConnectButtons();
        if (raiseAmountInput != null)
        {
            raiseAmountInput.onValueChanged.RemoveListener(ValidateRaiseInput);
            raiseAmountInput.onValueChanged.AddListener(ValidateRaiseInput);
        }

        HideAllCards();
        if (randomizeNpcGoldOnSceneStart) RandomizeNpcBankrolls();
        SetGameplayUIVisible(false);
        if (startPokerButton != null) startPokerButton.gameObject.SetActive(true);
        UpdateUI();
    }

    private void ConnectButtons()
    {
        AddButton(startPokerButton, StartPoker);
        AddButton(exitPokerButton, ExitPoker);
        AddButton(chip1Button, () => SelectChip(1));
        AddButton(chip5Button, () => SelectChip(5));
        AddButton(chip10Button, () => SelectChip(10));
        AddButton(chip50Button, () => SelectChip(50));
        AddButton(chip100Button, () => SelectChip(100));
        AddButton(chip500Button, () => SelectChip(500));
        AddButton(chip1KButton, () => SelectChip(1000));
        AddButton(chip5KButton, () => SelectChip(5000));
        AddButton(chip10KButton, () => SelectChip(10000));
        AddButton(chip50KButton, () => SelectChip(50000));
        AddButton(betButton, PlayerBet);
        AddButton(raiseButton, PlayerRaise);
        AddButton(callButton, PlayerCheckCall);
        AddButton(foldButton, PlayerFold);
    }

    private void AddButton(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button == null) return;
        button.onClick.RemoveListener(action);
        button.onClick.AddListener(action);
    }

    private void RandomizeNpcBankrolls()
    {
        int min = Mathf.Max(0, npcMinGold);
        int max = Mathf.Max(min, npcMaxGold);
        for (int i = 1; i < SeatCount; i++)
            stack[i] = UnityEngine.Random.Range(min, max + 1);
    }

    public void StartPoker()
    {
        if (handActive) return;
        if (GoldManager.Instance == null)
        {
            ShowMessage("Lỗi: không tìm thấy GoldManager.");
            return;
        }
        if (!ValidateSetup()) return;
        if (GoldManager.Instance.Gold <= 0)
        {
            ShowMessage("Bạn không còn vàng để chơi.");
            return;
        }

        if (gameCoroutine != null) StopCoroutine(gameCoroutine);
        gameCoroutine = null;

        hasFinishedHand = false;
        handActive = true;
        waitingForPlayer = false;
        playerSubmittedAction = false;
        playerGoldAtStart = GoldManager.Instance.Gold;
        playerTotalInvested = 0;
        playerPayout = 0;
        stack[0] = playerGoldAtStart;
        ResetHandArrays();

        BuildDeck();
        ShuffleDeck();
        DealHoleCards();
        boardCount = 0;
        pot = 0;
        currentBet = 0;
        lastFullRaise = Mathf.Max(1, bigBlind);
        selectedBet = 0;
        raiseCount = 0;
        dealerSeat = UnityEngine.Random.Range(0, SeatCount); // random button each hand
        street = Street.PreFlop;
        HideAllCards();
        RenderPlayerCards();
        RenderNpcCardBacks();
        RenderBoardBacks();
        SetGameplayUIVisible(true);
        if (startPokerButton != null) startPokerButton.gameObject.SetActive(false);
        if (handReportText != null) handReportText.text = "";
        ClearInput();
        UpdateUI();

        gameCoroutine = StartCoroutine(PlayHand());
    }

    private void ResetHandArrays()
    {
        for (int i = 0; i < SeatCount; i++)
        {
            streetContribution[i] = 0;
            handContribution[i] = 0;
            folded[i] = false;
            allIn[i] = false;
            actedSinceLastRaise[i] = false;
        }
        for (int i = 1; i < SeatCount; i++)
            allIn[i] = stack[i] <= 0;
        allIn[0] = stack[0] <= 0;
    }

    private bool ValidateSetup()
    {
        if (pokerDeck == null || pokerDeck.cards == null ||
            pokerDeck.cards.Length != 52 || pokerDeck.cardBack == null)
        {
            ShowMessage("Gán PokerDeck có đúng 52 lá và cardBack.");
            return false;
        }
        if (!ValidSlots(playerCards, 2) || !ValidSlots(npcCards, 8) ||
            !ValidSlots(communityCards, 5))
        {
            ShowMessage("Cần gán đủ 2 ô bài bạn, 8 ô NPC và 5 ô bài chung.");
            return false;
        }
        if (npcNames == null || npcNames.Length != 4 ||
            npcNames.Any(string.IsNullOrWhiteSpace) ||
            npcNames.Select(n => n.Trim().ToLowerInvariant()).Distinct().Count() != 4)
        {
            ShowMessage("Cần 4 tên NPC không trùng nhau.");
            return false;
        }

        HashSet<Sprite> unique = new HashSet<Sprite>();
        foreach (Sprite sprite in pokerDeck.cards)
        {
            if (sprite == null || !unique.Add(sprite))
            {
                ShowMessage("PokerDeck có Sprite trống hoặc trùng.");
                return false;
            }
            if (!TryParseCard(sprite, out _))
            {
                ShowMessage("Không đọc được tên Sprite '" + sprite.name +
                    "'. Đặt tên theo dạng AS, 10H, QD, KC, 2C. " +
                    "S=bích, H=cơ, D=rô, C=tép.");
                return false;
            }
        }
        return true;
    }

    private bool ValidSlots(Image[] slots, int count)
    {
        return slots != null && slots.Length == count && slots.All(s => s != null);
    }

    private void BuildDeck()
    {
        deck.Clear();
        foreach (Sprite sprite in pokerDeck.cards)
        {
            TryParseCard(sprite, out Card card);
            deck.Add(card);
        }
    }

    private void ShuffleDeck()
    {
        for (int i = deck.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            (deck[i], deck[j]) = (deck[j], deck[i]);
        }
    }

    private Card DrawCard()
    {
        if (deck.Count == 0) throw new InvalidOperationException("Bộ bài đã hết.");
        Card card = deck[0];
        deck.RemoveAt(0);
        return card;
    }

    private void DealHoleCards()
    {
        // Deal one card at a time around the table, as in Hold'em.
        for (int round = 0; round < 2; round++)
        {
            for (int offset = 1; offset <= SeatCount; offset++)
            {
                int seat = (dealerSeat + offset) % SeatCount;
                if (seat == 0) playerHole[round] = DrawCard();
                else npcHole[seat - 1][round] = DrawCard();
            }
        }
    }

    private IEnumerator PlayHand()
    {
        ShowMessage("Ván mới. Dealer ngẫu nhiên: " + PlayerName(dealerSeat) +
            ". Vàng NPC khác nhau và được giữ qua các ván.");
        yield return WaitNpcThink();

        // Heads-up blind/button details are not relevant with 5 seats; blinds rotate from button.
        int smallBlindSeat = NextEligibleSeat(dealerSeat);
        bigBlindSeatForHand = NextEligibleSeat(smallBlindSeat);
        PostBlind(smallBlindSeat, Mathf.Max(1, smallBlind));
        PostBlind(bigBlindSeatForHand, Mathf.Max(1, bigBlind));
        currentBet = Mathf.Max(currentBet, streetContribution[smallBlindSeat]);
        lastFullRaise = Mathf.Max(1, bigBlind);
        UpdateUI();

        yield return StartCoroutine(RunBettingRound(true));
        if (!handActive) yield break;
        if (CountLivePlayers() == 1) { AwardUncontestedPot(); yield break; }

        street = Street.Flop;
        yield return RevealBoardTo(3);
        if (!handActive) yield break;
        yield return StartCoroutine(RunBettingRound(false));
        if (!handActive) yield break;
        if (CountLivePlayers() == 1) { AwardUncontestedPot(); yield break; }

        street = Street.Turn;
        yield return RevealBoardTo(4);
        if (!handActive) yield break;
        yield return StartCoroutine(RunBettingRound(false));
        if (!handActive) yield break;
        if (CountLivePlayers() == 1) { AwardUncontestedPot(); yield break; }

        street = Street.River;
        yield return RevealBoardTo(5);
        if (!handActive) yield break;
        yield return StartCoroutine(RunBettingRound(false));
        if (!handActive) yield break;
        if (CountLivePlayers() == 1) { AwardUncontestedPot(); yield break; }

        street = Street.Showdown;
        ShowdownAndAward();
    }

    private void PostBlind(int seat, int blind)
    {
        if (folded[seat] || allIn[seat]) return;
        int paid = Mathf.Min(blind, stack[seat]);
        CommitChips(seat, paid);
        if (stack[seat] == 0) allIn[seat] = true;
        ShowMessage(PlayerName(seat) + " đặt blind " + F(paid) + ".");
    }

    private IEnumerator RunBettingRound(bool preFlop)
    {
        currentBet = streetContribution.Max();
        lastFullRaise = Mathf.Max(1, bigBlind);
        raiseCount = 0;
        for (int i = 0; i < SeatCount; i++)
            actedSinceLastRaise[i] = folded[i] || allIn[i];

        // On pre-flop, the big blind has already acted by posting; still gets a turn
        // if action returns to them and there was no raise.
        int firstSeat = preFlop ? NextSeat(bigBlindSeatForHand) : NextSeat(dealerSeat);
        int actor = (firstSeat - 1 + SeatCount) % SeatCount;
        int safety = 0;

        while (handActive && safety++ < 100)
        {
            if (CountLivePlayers() <= 1) yield break;
            if (BettingRoundComplete()) yield break;

            actor = FindNextActionSeat(actor);
            if (actor < 0) yield break;

            if (actor == 0)
            {
                waitingForPlayer = true;
                playerSubmittedAction = false;
                ShowMessage("Đến lượt bạn (" + StreetName() + "). Cần theo: " +
                    F(Mathf.Max(0, currentBet - streetContribution[0])) +
                    ". Pot: " + F(pot) + ". Vàng: " + F(stack[0]));
                SetActionButtonsInteractable(true);
                while (waitingForPlayer && !playerSubmittedAction && handActive)
                    yield return null;
                SetActionButtonsInteractable(false);
                waitingForPlayer = false;
                if (!handActive) yield break;
                ApplyAction(0, submittedAction, submittedAmount);
            }
            else
            {
                yield return WaitNpcThink();
                if (!handActive) yield break;
                NPCChooseAction(actor, out ActionKind action, out int amount);
                ApplyAction(actor, action, amount);
            }

            UpdateUI();
            if (CountLivePlayers() <= 1) yield break;
            yield return null;
        }
    }

    private int NextEligibleSeat(int from)
    {
        for (int step = 1; step <= SeatCount; step++)
        {
            int s = (from + step) % SeatCount;
            if (stack[s] > 0 && !folded[s]) return s;
        }
        return (from + 1) % SeatCount;
    }

    private int NextSeat(int from)
    {
        return (from + 1) % SeatCount;
    }

    private int FindNextActionSeat(int afterSeat)
    {
        for (int step = 1; step <= SeatCount; step++)
        {
            int s = (afterSeat + step) % SeatCount;
            if (folded[s] || allIn[s]) continue;
            if (!actedSinceLastRaise[s] || streetContribution[s] < currentBet)
                return s;
        }
        return -1;
    }

    private bool BettingRoundComplete()
    {
        for (int i = 0; i < SeatCount; i++)
        {
            if (folded[i] || allIn[i]) continue;
            if (!actedSinceLastRaise[i]) return false;
            if (streetContribution[i] != currentBet) return false;
        }
        return true;
    }

    private void NPCChooseAction(int seat, out ActionKind action, out int amount)
    {
        int toCall = Mathf.Max(0, currentBet - streetContribution[seat]);
        float roll = UnityEngine.Random.value;

        if (toCall > 0 && roll < npcFoldChanceFacingBet)
        {
            action = ActionKind.Fold;
            amount = 0;
            return;
        }

        bool canRaise = raiseCount < maximumRaisesPerStreet && stack[seat] > toCall;
        if (canRaise && roll < npcFoldChanceFacingBet + npcRaiseChance)
        {
            int raiseBy = UnityEngine.Random.Range(
                Mathf.Max(1, npcRaiseMin), Mathf.Max(npcRaiseMin, npcRaiseMax) + 1);
            // Raise amount means extra chips above the call. Cap to stack.
            raiseBy = Mathf.Min(raiseBy, stack[seat] - toCall);
            if (raiseBy > 0)
            {
                action = ActionKind.BetRaise;
                amount = toCall + raiseBy;
                return;
            }
        }

        action = ActionKind.CheckCall;
        amount = toCall;
    }

    private void ApplyAction(int seat, ActionKind action, int amount)
    {
        if (folded[seat] || allIn[seat]) return;
        int toCall = Mathf.Max(0, currentBet - streetContribution[seat]);

        if (action == ActionKind.Fold)
        {
            folded[seat] = true;
            actedSinceLastRaise[seat] = true;
            ShowMessage(PlayerName(seat) + " bỏ bài.");
            return;
        }

        if (action == ActionKind.CheckCall)
        {
            int paid = Mathf.Min(toCall, stack[seat]);
            CommitChips(seat, paid);
            if (stack[seat] == 0) allIn[seat] = true;
            actedSinceLastRaise[seat] = true;
            ShowMessage(paid == 0
                ? PlayerName(seat) + " check."
                : PlayerName(seat) + " theo " + F(paid) + " vàng.");
            return;
        }

        int requested = Mathf.Max(toCall, amount);
        int committed = Mathf.Min(requested, stack[seat]);
        int oldCurrentBet = currentBet;
        CommitChips(seat, committed);
        if (stack[seat] == 0) allIn[seat] = true;

        currentBet = Mathf.Max(currentBet, streetContribution[seat]);
        int raiseSize = currentBet - oldCurrentBet;
        if (raiseSize > 0)
        {
            raiseCount++;
            if (raiseSize >= lastFullRaise)
            {
                lastFullRaise = raiseSize;
                for (int i = 0; i < SeatCount; i++)
                    if (i != seat && !folded[i] && !allIn[i])
                        actedSinceLastRaise[i] = false;
            }
        }
        actedSinceLastRaise[seat] = true;

        ShowMessage(PlayerName(seat) + (oldCurrentBet == 0 ? " cược " : " tố ") +
            F(Mathf.Max(0, currentBet - oldCurrentBet)) +
            ". Mức cược vòng: " + F(currentBet) + ".");
    }

    private void CommitChips(int seat, int amount)
    {
        amount = Mathf.Clamp(amount, 0, stack[seat]);
        if (amount == 0) return;

        stack[seat] -= amount;
        streetContribution[seat] += amount;
        handContribution[seat] += amount;
        pot += amount;

        if (seat == 0)
        {
            playerTotalInvested += amount;
            if (GoldManager.Instance != null)
                GoldManager.Instance.SpendGold(amount);
        }
    }

    private void PlayerBet()
    {
        if (!CanPlayerAct()) return;
        int toCall = Mathf.Max(0, currentBet - streetContribution[0]);
        int desired = ReadSelectedAmount();
        if (currentBet > 0)
        {
            ShowMessage("Đã có cược. Dùng Theo hoặc Tố.");
            return;
        }
        if (desired < minimumBet)
        {
            ShowMessage("Cược tối thiểu là " + F(minimumBet) + " vàng.");
            return;
        }
        if (desired > stack[0])
        {
            ShowMessage("Bạn không đủ vàng.");
            return;
        }
        SubmitAction(ActionKind.BetRaise, desired);
    }

    private void PlayerRaise()
    {
        if (!CanPlayerAct()) return;
        int desired = ReadSelectedAmount();
        if (desired <= 0)
        {
            ShowMessage("Chọn chip hoặc nhập số tiền muốn tố.");
            return;
        }
        if (raiseCount >= maximumRaisesPerStreet)
        {
            ShowMessage("Đã đạt giới hạn số lần tố trong vòng này.");
            return;
        }

        int toCall = Mathf.Max(0, currentBet - streetContribution[0]);
        int total = toCall + desired;
        if (total > stack[0])
        {
            ShowMessage("Không đủ vàng để theo và tố số tiền này.");
            return;
        }
        if (desired < lastFullRaise && stack[0] > total)
        {
            ShowMessage("Mức tố tối thiểu thêm " + F(lastFullRaise) + " vàng.");
            return;
        }
        SubmitAction(ActionKind.BetRaise, total);
    }

    private void PlayerCheckCall()
    {
        if (!CanPlayerAct()) return;
        SubmitAction(ActionKind.CheckCall, 0);
    }

    private void PlayerFold()
    {
        if (!CanPlayerAct()) return;
        SubmitAction(ActionKind.Fold, 0);
    }

    private bool CanPlayerAct()
    {
        if (!handActive || !waitingForPlayer || playerSubmittedAction || folded[0] || allIn[0])
        {
            ShowMessage("Hiện chưa đến lượt bạn.");
            return false;
        }
        return true;
    }

    private void SubmitAction(ActionKind action, int amount)
    {
        submittedAction = action;
        submittedAmount = amount;
        playerSubmittedAction = true;
        waitingForPlayer = false;
        ClearSelection();
    }

    private int ReadSelectedAmount()
    {
        int amount = selectedBet;
        if (raiseAmountInput != null && !string.IsNullOrWhiteSpace(raiseAmountInput.text))
        {
            if (!int.TryParse(raiseAmountInput.text, out amount)) return -1;
        }
        return amount;
    }

    private void SelectChip(int value)
    {
        if (!CanPlayerAct()) return;
        int amount = ReadSelectedAmount();
        if (amount < 0 || amount + value > stack[0])
        {
            ShowMessage("Không đủ vàng để chọn chip này.");
            return;
        }
        selectedBet = amount + value;
        if (raiseAmountInput != null)
            raiseAmountInput.SetTextWithoutNotify(selectedBet.ToString());
        ShowMessage("Đã chọn " + F(selectedBet) + " vàng.");
    }

    private void ValidateRaiseInput(string input)
    {
        if (!handActive || !waitingForPlayer || string.IsNullOrWhiteSpace(input)) return;
        if (!int.TryParse(input, out int amount) || amount < 0)
        {
            ShowMessage("Nhập số vàng hợp lệ.");
            return;
        }
        if (amount > stack[0])
        {
            ShowMessage("Số tiền lớn hơn số vàng hiện có.");
            return;
        }
        selectedBet = amount;
    }

    private IEnumerator RevealBoardTo(int target)
    {
        while (boardCount < target)
        {
            board[boardCount] = DrawCard();
            ShowCard(communityCards[boardCount], board[boardCount].Sprite);
            boardCount++;
            yield return new WaitForSeconds(cardRevealDelay);
        }
        ShowMessage(target == 3 ? "Flop đã lật." : target == 4 ? "Turn đã lật." : "River đã lật.");
    }

    private void ShowdownAndAward()
    {
        // Reveal NPC hole cards.
        for (int i = 0; i < 8; i++)
            ShowCard(npcCards[i], npcHole[i / 2][i % 2].Sprite);

        StringBuilder report = new StringBuilder();
        report.AppendLine("KẾT QUẢ SHOWDOWN");
        report.AppendLine("Bài chung: " + string.Join(" ", board.Select(c => c.Name)));

        List<int> eligible = new List<int>();
        List<HandValue> values = new List<HandValue>();
        for (int seat = 0; seat < SeatCount; seat++)
        {
            if (folded[seat]) continue;
            Card[] hole = GetHoleCards(seat);
            HandValue value = EvaluateBestFive(hole.Concat(board).ToList());
            eligible.Add(seat);
            values.Add(value);
            report.AppendLine(PlayerName(seat) + ": " + hole[0].Name + " " + hole[1].Name +
                " — " + value.CategoryName + " | 5 lá tốt nhất: " +
                string.Join(" ", value.BestFive.Select(c => c.Name)) +
                " | So điểm: " + string.Join("-", value.TieBreak.Select(RankText)));
        }

        AwardPots(report, eligible, values);
    }

    private void AwardPots(StringBuilder report, List<int> eligible, List<HandValue> values)
    {
        // Side pots are constructed from each player's total contribution level.
        // Folded players contribute chips but cannot win any pot.
        int[] levels = handContribution.Where(v => v > 0).Distinct().OrderBy(v => v).ToArray();
        int remainingPot = pot;
        int totalPlayerPayout = 0;

        if (levels.Length == 0)
        {
            FinishHand(report.ToString() + "\nKhông có pot để chia.");
            return;
        }

        foreach (int level in levels)
        {
            int previous = levels.Where(x => x < level).DefaultIfEmpty(0).Max();
            int contributors = 0;
            for (int s = 0; s < SeatCount; s++)
                if (handContribution[s] >= level) contributors++;

            int sidePot = (level - previous) * contributors;
            sidePot = Mathf.Min(sidePot, remainingPot);
            remainingPot -= sidePot;

            List<int> contenders = new List<int>();
            for (int i = 0; i < eligible.Count; i++)
                if (handContribution[eligible[i]] >= level)
                    contenders.Add(i);

            if (contenders.Count == 0)
                continue;

            int bestIndex = contenders[0];
            List<int> winningIndices = new List<int> { bestIndex };
            for (int c = 1; c < contenders.Count; c++)
            {
                int idx = contenders[c];
                int cmp = values[idx].CompareTo(values[bestIndex]);
                if (cmp > 0)
                {
                    bestIndex = idx;
                    winningIndices.Clear();
                    winningIndices.Add(idx);
                }
                else if (cmp == 0)
                {
                    winningIndices.Add(idx);
                }
            }

            int each = sidePot / winningIndices.Count;
            int remainder = sidePot % winningIndices.Count;
            List<string> winnerNames = new List<string>();
            foreach (int winnerIndex in winningIndices)
            {
                int seat = eligible[winnerIndex];
                int payout = each + (remainder > 0 ? 1 : 0);
                if (remainder > 0) remainder--;
                stack[seat] += payout;
                if (seat == 0) totalPlayerPayout += payout;
                winnerNames.Add(PlayerName(seat));
            }
            report.AppendLine("Pot " + F(sidePot) + " thắng: " + string.Join(", ", winnerNames));
        }

        playerPayout = totalPlayerPayout;
        if (playerPayout > 0 && GoldManager.Instance != null)
            GoldManager.Instance.AddGold(playerPayout);

        int net = playerPayout - playerTotalInvested;
        report.AppendLine();
        report.AppendLine("TỔNG POT: " + F(pot));
        report.AppendLine("Bạn đã cược: " + F(playerTotalInvested));
        report.AppendLine("Tiền nhận về: " + F(playerPayout));
        report.AppendLine(net >= 0 ? "LÃI RÒNG: +" + F(net) : "LỖ RÒNG: -" + F(-net));
        report.AppendLine("Vàng hiện tại: " + F(GoldManager.Instance != null ? GoldManager.Instance.Gold : 0));
        report.AppendLine("Số dư NPC:");
        for (int i = 1; i < SeatCount; i++)
            report.AppendLine(PlayerName(i) + ": " + F(stack[i]) + " vàng");

        FinishHand(report.ToString());
    }

    private void AwardUncontestedPot()
    {
        int winner = -1;
        for (int i = 0; i < SeatCount; i++)
            if (!folded[i]) { winner = i; break; }

        StringBuilder report = new StringBuilder();
        report.AppendLine(PlayerName(winner) + " thắng vì tất cả đối thủ khác đã bỏ bài.");
        report.AppendLine("Không cần so bài khi chỉ còn một người.");
        report.AppendLine("Pot: " + F(pot));

        if (winner >= 0)
        {
            stack[winner] += pot;
            if (winner == 0)
            {
                playerPayout = pot;
                if (GoldManager.Instance != null) GoldManager.Instance.AddGold(pot);
            }
        }

        int net = playerPayout - playerTotalInvested;
        report.AppendLine("Bạn đã cược: " + F(playerTotalInvested));
        report.AppendLine("Tiền nhận về: " + F(playerPayout));
        report.AppendLine(net >= 0 ? "LÃI RÒNG: +" + F(net) : "LỖ RÒNG: -" + F(-net));
        report.AppendLine("Vàng hiện tại: " + F(GoldManager.Instance != null ? GoldManager.Instance.Gold : 0));
        for (int i = 1; i < SeatCount; i++)
            report.AppendLine(PlayerName(i) + ": " + F(stack[i]) + " vàng");
        FinishHand(report.ToString());
    }

    private void FinishHand(string report)
    {
        if (!handActive) return;
        handActive = false;
        waitingForPlayer = false;
        hasFinishedHand = true;
        SetActionButtonsInteractable(false);
        SetGameplayUIVisible(false);
        if (dealerMessageText != null)
        {
            dealerMessageText.gameObject.SetActive(true);
            dealerMessageText.text = report;
        }
        if (handReportText != null)
        {
            handReportText.gameObject.SetActive(true);
            handReportText.text = report;
        }
        if (startPokerButton != null)
        {
            startPokerButton.gameObject.SetActive(true);
            TMP_Text label = startPokerButton.GetComponentInChildren<TMP_Text>();
            if (label != null) label.text = "BẮT ĐẦU VÁN MỚI";
        }
        UpdateUI();
        gameCoroutine = null;
    }

    private int CountLivePlayers()
    {
        int count = 0;
        for (int i = 0; i < SeatCount; i++)
            if (!folded[i]) count++;
        return count;
    }

    private Card[] GetHoleCards(int seat) => seat == 0 ? playerHole : npcHole[seat - 1];

    private void RenderPlayerCards()
    {
        for (int i = 0; i < 2; i++) ShowCard(playerCards[i], playerHole[i].Sprite);
    }

    private void RenderNpcCardBacks()
    {
        for (int i = 0; i < npcCards.Length; i++) ShowCard(npcCards[i], pokerDeck.cardBack);
    }

    private void RenderBoardBacks()
    {
        for (int i = 0; i < communityCards.Length; i++) ShowCard(communityCards[i], pokerDeck.cardBack);
    }

    private void ShowCard(Image slot, Sprite sprite)
    {
        if (slot == null || sprite == null) return;
        slot.sprite = sprite;
        slot.preserveAspect = true;
        slot.gameObject.SetActive(true);
    }

    private void HideAllCards()
    {
        HideSlots(playerCards);
        HideSlots(npcCards);
        HideSlots(communityCards);
    }

    private void HideSlots(Image[] slots)
    {
        if (slots == null) return;
        foreach (Image slot in slots)
            if (slot != null) slot.gameObject.SetActive(false);
    }

    private void SetGameplayUIVisible(bool visible)
    {
        SetButtonVisible(chip1Button, visible);
        SetButtonVisible(chip5Button, visible);
        SetButtonVisible(chip10Button, visible);
        SetButtonVisible(chip50Button, visible);
        SetButtonVisible(chip100Button, visible);
        SetButtonVisible(chip500Button, visible);
        SetButtonVisible(chip1KButton, visible);
        SetButtonVisible(chip5KButton, visible);
        SetButtonVisible(chip10KButton, visible);
        SetButtonVisible(chip50KButton, visible);
        SetButtonVisible(betButton, visible);
        SetButtonVisible(callButton, visible);
        SetButtonVisible(raiseButton, visible);
        SetButtonVisible(foldButton, visible);
        SetButtonVisible(exitPokerButton, visible);
        if (raiseAmountInput != null) raiseAmountInput.gameObject.SetActive(visible);
        if (currentBetText != null) currentBetText.gameObject.SetActive(visible);
        if (goldAmountText != null) goldAmountText.gameObject.SetActive(visible);
        if (dealerMessageText != null)
            dealerMessageText.gameObject.SetActive(visible || hasFinishedHand);
    }

    private void SetButtonVisible(Button button, bool visible)
    {
        if (button != null) button.gameObject.SetActive(visible);
    }

    private void SetActionButtonsInteractable(bool value)
    {
        if (betButton != null) betButton.interactable = value;
        if (callButton != null) callButton.interactable = value;
        if (raiseButton != null) raiseButton.interactable = value;
        if (foldButton != null) foldButton.interactable = value;
        if (chip1Button != null) chip1Button.interactable = value;
        if (chip5Button != null) chip5Button.interactable = value;
        if (chip10Button != null) chip10Button.interactable = value;
        if (chip50Button != null) chip50Button.interactable = value;
        if (chip100Button != null) chip100Button.interactable = value;
        if (chip500Button != null) chip500Button.interactable = value;
        if (chip1KButton != null) chip1KButton.interactable = value;
        if (chip5KButton != null) chip5KButton.interactable = value;
        if (chip10KButton != null) chip10KButton.interactable = value;
        if (chip50KButton != null) chip50KButton.interactable = value;
    }

    private IEnumerator WaitNpcThink()
    {
        float min = Mathf.Clamp(npcMinThinkTime, 0.1f, 10f);
        float max = Mathf.Clamp(npcMaxThinkTime, min, 10f);
        yield return new WaitForSeconds(UnityEngine.Random.Range(min, max));
    }

    private bool TryParseCard(Sprite sprite, out Card card)
    {
        card = null;
        if (sprite == null) return false;
        string name = sprite.name.Replace("_", "").Replace("-", "").Replace(" ", "").ToUpperInvariant();
        if (name.Length < 2) return false;
        char suit = name[name.Length - 1];
        if ("SHDC".IndexOf(suit) < 0) return false;
        string rankPart = name.Substring(0, name.Length - 1);
        int rank;
        switch (rankPart)
        {
            case "A": case "ACE": rank = 14; break;
            case "K": case "KING": rank = 13; break;
            case "Q": case "QUEEN": rank = 12; break;
            case "J": case "JACK": rank = 11; break;
            default:
                if (!int.TryParse(rankPart, out rank)) return false;
                break;
        }
        if (rank < 2 || rank > 14) return false;
        card = new Card { Rank = rank, Suit = suit, Sprite = sprite };
        return true;
    }

    private HandValue EvaluateBestFive(List<Card> seven)
    {
        HandValue best = null;
        for (int a = 0; a < 3; a++)
            for (int b = a + 1; b < 4; b++)
                for (int c = b + 1; c < 5; c++)
                    for (int d = c + 1; d < 6; d++)
                        for (int e = d + 1; e < 7; e++)
                        {
                            List<Card> five = new List<Card> { seven[a], seven[b], seven[c], seven[d], seven[e] };
                            HandValue value = EvaluateFive(five);
                            if (best == null || value.CompareTo(best) > 0) best = value;
                        }
        return best;
    }

    private HandValue EvaluateFive(List<Card> cards)
    {
        int[] ranks = cards.Select(c => c.Rank).OrderByDescending(v => v).ToArray();
        var groups = cards.GroupBy(c => c.Rank)
            .Select(g => new { Rank = g.Key, Count = g.Count() })
            .OrderByDescending(g => g.Count).ThenByDescending(g => g.Rank).ToList();
        bool flush = cards.All(c => c.Suit == cards[0].Suit);
        int straightHigh = StraightHigh(ranks);
        bool straight = straightHigh > 0;
        HandCategory category;
        int[] tie;

        if (flush && straight && straightHigh == 14 && ranks.Contains(10))
        { category = HandCategory.RoyalFlush; tie = new[] { 14 }; }
        else if (flush && straight)
        { category = HandCategory.StraightFlush; tie = new[] { straightHigh }; }
        else if (groups[0].Count == 4)
        { category = HandCategory.FourOfAKind; tie = new[] { groups[0].Rank, groups[1].Rank }; }
        else if (groups[0].Count == 3 && groups[1].Count == 2)
        { category = HandCategory.FullHouse; tie = new[] { groups[0].Rank, groups[1].Rank }; }
        else if (flush)
        { category = HandCategory.Flush; tie = ranks; }
        else if (straight)
        { category = HandCategory.Straight; tie = new[] { straightHigh }; }
        else if (groups[0].Count == 3)
        { category = HandCategory.ThreeOfAKind; tie = new[] { groups[0].Rank }.Concat(groups.Where(g => g.Count == 1).Select(g => g.Rank).OrderByDescending(v => v)).ToArray(); }
        else if (groups[0].Count == 2 && groups[1].Count == 2)
        {
            int highPair = Math.Max(groups[0].Rank, groups[1].Rank);
            int lowPair = Math.Min(groups[0].Rank, groups[1].Rank);
            int kicker = groups.First(g => g.Count == 1).Rank;
            category = HandCategory.TwoPair;
            tie = new[] { highPair, lowPair, kicker };
        }
        else if (groups[0].Count == 2)
        {
            category = HandCategory.Pair;
            tie = new[] { groups[0].Rank }.Concat(
                groups.Where(g => g.Count == 1).Select(g => g.Rank).OrderByDescending(v => v)).ToArray();
        }
        else
        { category = HandCategory.HighCard; tie = ranks; }

        return new HandValue { Category = category, TieBreak = tie, BestFive = cards };
    }

    private int StraightHigh(int[] ranks)
    {
        int[] unique = ranks.Distinct().OrderByDescending(v => v).ToArray();
        if (unique.Contains(14))
            unique = unique.Concat(new[] { 1 }).OrderByDescending(v => v).ToArray();
        for (int i = 0; i <= unique.Length - 5; i++)
        {
            bool consecutive = true;
            for (int j = 0; j < 4; j++)
            {
                if (unique[i + j] - 1 != unique[i + j + 1])
                { consecutive = false; break; }
            }
            if (consecutive) return unique[i] == 1 ? 5 : unique[i];
        }
        return 0;
    }

    public void ExitPoker()
    {
        if (gameCoroutine != null)
        {
            StopCoroutine(gameCoroutine);
            gameCoroutine = null;
        }
        handActive = false;
        waitingForPlayer = false;
        playerSubmittedAction = false;
        HideAllCards();
        SetGameplayUIVisible(false);
        if (startPokerButton != null) startPokerButton.gameObject.SetActive(true);
        ClearSelection();
        if (pokerCanvas != null) pokerCanvas.SetActive(false);
        else ShowMessage("Hãy gán PokerCanvas trong Inspector để nút Thoát đóng giao diện.");
    }

    private void ClearSelection()
    {
        selectedBet = 0;
        ClearInput();
    }

    private void ClearInput()
    {
        if (raiseAmountInput != null) raiseAmountInput.SetTextWithoutNotify("");
    }

    private string PlayerName(int seat) => seat == 0 ? "Bạn" : npcNames[seat - 1];
    private string StreetName() => street switch
    {
        Street.PreFlop => "Pre-flop",
        Street.Flop => "Flop",
        Street.Turn => "Turn",
        Street.River => "River",
        _ => "Showdown"
    };
    private string F(int amount) => amount.ToString("N0");
    private static string RankText(int rank) => rank switch
    {
        14 => "A",
        13 => "K",
        12 => "Q",
        11 => "J",
        _ => rank.ToString()
    };
    private static string SuitText(char suit) => suit switch
    {
        'S' => "♠",
        'H' => "♥",
        'D' => "♦",
        _ => "♣"
    };

    private void UpdateUI()
    {
        if (currentBetText != null)
            currentBetText.text = "Cược vòng: " + F(currentBet) + " | Pot: " + F(pot);
        if (goldAmountText != null && GoldManager.Instance != null)
            goldAmountText.text = "Vàng: " + F(GoldManager.Instance.Gold);
    }

    private void ShowMessage(string message)
    {
        if (dealerMessageText != null)
        {
            dealerMessageText.gameObject.SetActive(true);
            dealerMessageText.text = message;
        }
        Debug.Log(message);
    }

    private void OnDestroy()
    {
        if (raiseAmountInput != null)
            raiseAmountInput.onValueChanged.RemoveListener(ValidateRaiseInput);
        RemoveButton(startPokerButton, StartPoker);
        RemoveButton(exitPokerButton, ExitPoker);
        RemoveButton(betButton, PlayerBet);
        RemoveButton(raiseButton, PlayerRaise);
        RemoveButton(callButton, PlayerCheckCall);
        RemoveButton(foldButton, PlayerFold);
        if (gameCoroutine != null) StopCoroutine(gameCoroutine);
    }

    private void RemoveButton(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button != null) button.onClick.RemoveListener(action);
    }
}
