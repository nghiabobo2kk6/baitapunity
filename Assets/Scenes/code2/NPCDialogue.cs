
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class NPCDialogue : MonoBehaviour
{
    [Header("NPC Dialogue UI")]
    public GameObject dialoguePanel;
    public Image npcPortrait;
    public TMP_Text dialogueText;
    public Button nextButton;
    public Sprite npcPortraitSprite;

    [Header("Player Dialogue UI")]
    public GameObject playerDialoguePanel;
    public TMP_Text playerDialogueText;
    public Button playerNextButton;

    [Header("NPC Lines")]
    [TextArea(2, 4)]
    public string[] npcLines =
    {
        "Chao ban! Ban moi den vung ngoai o Bargtok a?",
        "Toi dang can mot chut giup do."
    };

    [Header("Player Lines")]
    [TextArea(2, 4)]
    public string[] playerLines =
    {
        "Dung vay, toi dang tham quan noi nay.",
        "Toi co the giup gi cho ban?"
    };

    private Collider2D npcCollider;
    private Camera mainCamera;

    private int currentLine;
    private bool dialogueOpen;
    private bool playerSpeaking;

    private static NPCDialogue activeDialogue;

    private void Awake()
    {
        npcCollider = GetComponent<Collider2D>();
        mainCamera = Camera.main;

        if (dialoguePanel != null)
            dialoguePanel.SetActive(false);

        if (playerDialoguePanel != null)
            playerDialoguePanel.SetActive(false);

        if (nextButton != null)
        {
            nextButton.onClick.RemoveListener(NextLine);
            nextButton.onClick.AddListener(NextLine);
        }

        if (playerNextButton != null)
        {
            playerNextButton.onClick.RemoveListener(NextLine);
            playerNextButton.onClick.AddListener(NextLine);
        }
    }

    private void Update()
    {
        if (activeDialogue != null && activeDialogue != this)
            return;

        if (dialogueOpen)
            return;

        if (Input.GetMouseButtonDown(0))
            CheckClickNPC();
    }

    private void CheckClickNPC()
    {
        if (EventSystem.current != null &&
            EventSystem.current.IsPointerOverGameObject())
            return;

        if (mainCamera == null)
            mainCamera = Camera.main;

        if (mainCamera == null || npcCollider == null)
            return;

        Vector2 point =
            mainCamera.ScreenToWorldPoint(Input.mousePosition);

        if (npcCollider.OverlapPoint(point))
            StartDialogue();
    }

    private void StartDialogue()
    {
        if (npcLines == null || playerLines == null ||
            npcLines.Length == 0 ||
            npcLines.Length != playerLines.Length)
        {
            Debug.LogError(
                name + ": NPC Lines va Player Lines phai co cung so cau.",
                this);
            return;
        }

        if (dialoguePanel == null ||
            dialogueText == null || nextButton == null ||
            playerDialoguePanel == null ||
            playerDialogueText == null ||
            playerNextButton == null)
        {
            Debug.LogError(
                name + ": Hay gan day du cac tham chieu UI trong Inspector.",
                this);
            return;
        }

        if (activeDialogue != null && activeDialogue != this)
            activeDialogue.CloseDialogue();

        activeDialogue = this;
        dialogueOpen = true;
        playerSpeaking = false;
        currentLine = 0;

        ShowNpcLine();
    }

    public void NextLine()
    {
        if (!dialogueOpen)
            return;

        if (!playerSpeaking)
        {
            // NPC has spoken. Show the player's reply.
            dialoguePanel.SetActive(false);
            playerSpeaking = true;
            ShowPlayerLine();
            return;
        }

        // Player has replied. Move to the next conversation pair.
        currentLine++;

        if (currentLine >= npcLines.Length)
        {
            CloseDialogue();
            return;
        }

        playerDialoguePanel.SetActive(false);
        playerSpeaking = false;
        ShowNpcLine();
    }

    private void ShowNpcLine()
    {
        playerDialoguePanel.SetActive(false);
        dialoguePanel.SetActive(true);

        if (npcPortrait != null)
        {
            npcPortrait.sprite = npcPortraitSprite;
            npcPortrait.gameObject.SetActive(npcPortraitSprite != null);
        }

        dialogueText.text = npcLines[currentLine];
        nextButton.gameObject.SetActive(true);
    }

    private void ShowPlayerLine()
    {
        dialoguePanel.SetActive(false);
        playerDialoguePanel.SetActive(true);

        playerDialogueText.text = playerLines[currentLine];
        playerNextButton.gameObject.SetActive(true);
    }

    private void CloseDialogue()
    {
        dialogueOpen = false;

        if (dialoguePanel != null)
            dialoguePanel.SetActive(false);

        if (playerDialoguePanel != null)
            playerDialoguePanel.SetActive(false);

        if (activeDialogue == this)
            activeDialogue = null;
    }

    private void OnDisable()
    {
        if (activeDialogue == this)
            activeDialogue = null;
    }
}
