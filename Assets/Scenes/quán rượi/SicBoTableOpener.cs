
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SicBoTableOpener : MonoBehaviour
{
    [Header("SicBo Objects")]
    [SerializeField] private GameObject sicBoCanvas;
    [SerializeField] private GameObject startSicBoButton;

    [Header("Click Blocking")]
    [SerializeField] private GraphicRaycaster sicBoRaycaster;
    [SerializeField] private CanvasGroup sicBoCanvasGroup;

    private void Awake()
    {
        if (sicBoCanvas != null)
            sicBoCanvas.SetActive(false);
    }

    private void OnMouseDown()
    {
        // Không mở bàn khi nhấn lên một UI đang hoạt động.
        if (EventSystem.current != null &&
            EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }

        OpenSicBo();
    }

    private void OpenSicBo()
    {
        if (sicBoCanvas == null)
        {
            Debug.LogError("SicBoTableOpener: Chưa gán SicBoCanvas!");
            return;
        }

        sicBoCanvas.SetActive(true);

        if (startSicBoButton != null)
            startSicBoButton.SetActive(true);

        // Cho Canvas SicBo chặn raycast UI.
        if (sicBoRaycaster == null)
            sicBoRaycaster = sicBoCanvas.GetComponent<GraphicRaycaster>();

        if (sicBoCanvasGroup == null)
            sicBoCanvasGroup = sicBoCanvas.GetComponent<CanvasGroup>();

        if (sicBoCanvasGroup != null)
        {
            sicBoCanvasGroup.interactable = true;
            sicBoCanvasGroup.blocksRaycasts = true;
        }

        if (sicBoRaycaster != null)
            sicBoRaycaster.enabled = true;
    }

    public void CloseSicBo()
    {
        if (sicBoCanvas != null)
            sicBoCanvas.SetActive(false);
    }
}
