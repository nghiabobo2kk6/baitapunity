
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RoyalLetter : MonoBehaviour
{
    [Header("Small Letter In Scene")]
    public SpriteRenderer letterRenderer;
    public Collider2D letterCollider;

    [Header("Large Letter UI")]
    public GameObject letterPanel;
    public TMP_Text letterText;

    [Header("Other UI To Hide")]
    public GameObject dialoguePanel;
    public GameObject kingPortrait;

    [Header("Story: The Missing Treasure")]
    [TextArea(3, 6)]
    public string[] letterLines =
    {
        "Gui nha vua,",
        "Neu nguoi dang doc buc thu nay, nghia la bau vat hoang gia da bien mat. Ta da biet chuyen nay tu truoc khi cung dien dong cua.",
        "Bau vat khong chi la vang bac. No la vat an chua loi the bao ve vuong quoc, duoc truyen lai tu doi vua dau tien.",
        "Co ke phan boi dang an minh trong cung dien. Dung tin bat ky ai, ke ca nhung nguoi mang huy hieu hoang gia.",
        "Hay tim den can phong bi niem phong duoi thap phia Dong. Chiec chia khoa nam trong tay mot nguoi ma nguoi van tin tuong.",
        "Neu bau vat khong duoc tim thay truoc dem trang, vuong quoc se roi vao hoan loan.",
        "Hay tha thu cho ta vi da giu kin bi mat nay. Ta chi mong vuong quoc van con ton tai.",
        "Nguoi ban cu, nguoi da bao ve ngai vang tu nhung ngay dau tien."
    };

    private Button nextButton;
    private int currentLine;
    private bool isOpened;
    private bool isFinished;
    private bool isVisible;

    private void Awake()
    {
        if (letterRenderer == null)
            letterRenderer = GetComponent<SpriteRenderer>();

        if (letterCollider == null)
            letterCollider = GetComponent<Collider2D>();

        // Find the panel if it was not assigned.
        if (letterPanel == null)
        {
            Canvas canvas = FindObjectOfType<Canvas>();

            if (canvas != null)
            {
                Transform panelTransform =
                    canvas.transform.Find("LetterPanel");

                if (panelTransform != null)
                    letterPanel = panelTransform.gameObject;
            }
        }

        // Find the letter text inside the panel.
        if (letterPanel != null && letterText == null)
            letterText = letterPanel.GetComponentInChildren<TMP_Text>(true);

        // Find the Continue button anywhere under the Canvas.
        Canvas parentCanvas = FindObjectOfType<Canvas>();

        if (parentCanvas != null)
        {
            Button[] buttons =
                parentCanvas.GetComponentsInChildren<Button>(true);

            foreach (Button button in buttons)
            {
                if (button.transform.IsChildOf(letterPanel != null
                    ? letterPanel.transform
                    : parentCanvas.transform))
                {
                    nextButton = button;
                    break;
                }
            }
        }

        if (nextButton != null)
        {
            nextButton.onClick.RemoveListener(NextLine);
            nextButton.onClick.AddListener(NextLine);
        }
        else
        {
            Debug.LogError(
                "RoyalLetter: Cannot find the letter Continue button.",
                this
            );
        }

        if (letterPanel != null)
            letterPanel.SetActive(false);
        else
            Debug.LogError("RoyalLetter: LetterPanel was not found.", this);
    }

    private void OnMouseDown()
    {
        if (isOpened || letterPanel == null)
            return;

        isOpened = true;
        isVisible = true;
        isFinished = false;
        currentLine = 0;

        if (letterRenderer != null)
            letterRenderer.enabled = false;

        if (letterCollider != null)
            letterCollider.enabled = false;

        if (dialoguePanel != null)
            dialoguePanel.SetActive(false);

        if (kingPortrait != null)
            kingPortrait.SetActive(false);

        letterPanel.SetActive(true);

        if (nextButton != null)
            nextButton.gameObject.SetActive(true);

        ShowCurrentLine();
    }

    public void NextLine()
    {
        if (!isVisible || isFinished)
            return;

        currentLine++;

        if (letterLines == null || currentLine >= letterLines.Length)
        {
            isFinished = true;

            if (letterText != null)
                letterText.text = "Het buc thu.";

            if (nextButton != null)
                nextButton.gameObject.SetActive(false);

            return;
        }

        ShowCurrentLine();
    }

    private void ShowCurrentLine()
    {
        if (letterText != null &&
            letterLines != null &&
            currentLine >= 0 &&
            currentLine < letterLines.Length)
        {
            letterText.text = letterLines[currentLine];
        }
    }

    private void Update()
    {
        if (!isVisible || !isFinished)
            return;

        if (Input.GetMouseButtonDown(0))
        {
            if (IsMouseInsideLetter())
                return;

            CloseLetter();
        }
    }

    private bool IsMouseInsideLetter()
    {
        if (letterPanel == null)
            return false;

        RectTransform rect =
            letterPanel.GetComponent<RectTransform>();

        return rect != null &&
            RectTransformUtility.RectangleContainsScreenPoint(
                rect, Input.mousePosition, null);
    }

    private void CloseLetter()
    {
        isVisible = false;

        if (letterPanel != null)
            letterPanel.SetActive(false);
    }
}
