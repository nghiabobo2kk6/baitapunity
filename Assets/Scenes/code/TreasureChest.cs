using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class TreasureChest : MonoBehaviour
{
    [Header("Dialogue UI")]
    public GameObject dialoguePanel;
    public GameObject kingPortrait;
    public TMP_Text dialogueText;
    public Button nextButton;

    [Header("Story")]
    [TextArea(2, 4)]
    public string[] storyLines =
    {
        "Nhà vua: Cuối cùng chiếc rương cũng đã mở!",
        "Nhà vua: Khoan đã... BÁU VẬT ĐÃ BIẾN MẤT!",
        "Nhà vua: Ai đã lấy báu vật của vương quốc?",
        "Nhà vua: Phải tìm ra sự thật bằng mọi giá!"
    };

    private SpriteRenderer chestRenderer;
    private Collider2D chestCollider;
    private Camera mainCamera;

    private int currentLine = 0;

    private bool isOpened = false;
    private bool storyFinished = false;
    private bool dialogueVisible = false;

    private bool waitForMouseRelease = false;

    private void Awake()
    {
        chestRenderer = GetComponent<SpriteRenderer>();
        chestCollider = GetComponent<Collider2D>();

        mainCamera = Camera.main;

        if (dialoguePanel != null)
            dialoguePanel.SetActive(false);

        if (kingPortrait != null)
            kingPortrait.SetActive(false);

        if (nextButton != null)
        {
            nextButton.onClick.RemoveListener(NextLine);
            nextButton.onClick.AddListener(NextLine);
        }
    }

    private void Start()
    {
        if (chestRenderer != null)
            chestRenderer.enabled = true;

        if (chestCollider != null)
            chestCollider.enabled = true;
        else
            Debug.LogError("TreasureChest: Chua co Collider2D!");

        if (mainCamera == null)
            Debug.LogError("TreasureChest: Khong tim thay Main Camera!");
    }

    private void Update()
    {
        if (waitForMouseRelease)
        {
            if (Input.GetMouseButtonUp(0))
                waitForMouseRelease = false;

            return;
        }

        // Chua mo ruong: kiem tra nhan chuot.
        if (!isOpened)
        {
            CheckChestClick();
            return;
        }

        // Da mo ruong: cho doc het hoi thoai.
        if (!dialogueVisible || !storyFinished)
            return;

        if (!Input.GetMouseButtonDown(0))
            return;

        // Khong dong khi nhan vao UI.
        if (EventSystem.current != null &&
            EventSystem.current.IsPointerOverGameObject())
            return;

        CloseDialogue();
    }

    private void CheckChestClick()
    {
        if (!Input.GetMouseButtonDown(0))
            return;

        if (mainCamera == null)
            mainCamera = Camera.main;

        if (mainCamera == null || chestCollider == null)
            return;

        // Khong mo ruong khi nhan vao UI.
        if (EventSystem.current != null &&
            EventSystem.current.IsPointerOverGameObject())
            return;

        Vector3 mousePosition = Input.mousePosition;

        Vector3 worldPosition =
            mainCamera.ScreenToWorldPoint(mousePosition);

        Vector2 clickPosition =
            new Vector2(worldPosition.x, worldPosition.y);

        // Kiem tra chuot co nam trong collider cua ruong.
        if (chestCollider.OverlapPoint(clickPosition))
        {
            Debug.Log("TreasureChest: Da nhan trung ruong!");

            OpenChest();
        }
    }

    private void OpenChest()
    {
        if (isOpened)
            return;

        if (storyLines == null || storyLines.Length == 0)
        {
            Debug.LogError("TreasureChest: Story Lines dang rong!");
            return;
        }

        isOpened = true;
        dialogueVisible = true;
        storyFinished = false;
        currentLine = 0;

        waitForMouseRelease = true;

        // Ruong bien mat.
        if (chestRenderer != null)
            chestRenderer.enabled = false;

        // Tat collider sau khi mo.
        if (chestCollider != null)
            chestCollider.enabled = false;

        // Hien bang hoi thoai.
        if (dialoguePanel != null)
            dialoguePanel.SetActive(true);

        if (kingPortrait != null)
            kingPortrait.SetActive(true);

        if (nextButton != null)
            nextButton.gameObject.SetActive(true);

        ShowCurrentLine();

        Debug.Log("TreasureChest: Da mo ruong va hien hoi thoai!");
    }

    public void NextLine()
    {
        if (!dialogueVisible || storyFinished)
            return;

        currentLine++;

        if (currentLine >= storyLines.Length)
        {
            storyFinished = true;

            if (nextButton != null)
                nextButton.gameObject.SetActive(false);

            Debug.Log("TreasureChest: Da doc het hoi thoai!");

            return;
        }

        ShowCurrentLine();
    }

    private void ShowCurrentLine()
    {
        if (dialogueText == null)
        {
            Debug.LogError("TreasureChest: Chua gan Dialogue Text!");
            return;
        }

        dialogueText.text = storyLines[currentLine];
    }

    private void CloseDialogue()
    {
        dialogueVisible = false;

        if (dialoguePanel != null)
            dialoguePanel.SetActive(false);

        if (kingPortrait != null)
            kingPortrait.SetActive(false);

        Debug.Log("TreasureChest: Da dong hoi thoai!");
    }
}