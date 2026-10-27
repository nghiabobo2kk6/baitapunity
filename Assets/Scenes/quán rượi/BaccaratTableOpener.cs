
using UnityEngine;

[DisallowMultipleComponent]
public class BaccaratTableOpener : MonoBehaviour
{
    [Header("Baccarat References")]
    [SerializeField] private BaccaratGameManager gameManager;
    [SerializeField] private GameObject baccaratCanvas;
    [SerializeField] private GameObject baccaratPanel;

    [Header("Click Detection")]
    [SerializeField] private Collider2D tableCollider;

    private Camera mainCamera;

    private void Awake()
    {
        mainCamera = Camera.main;

        if (tableCollider == null)
            tableCollider = GetComponent<Collider2D>();

        if (gameManager == null)
            gameManager = FindFirstObjectByType<BaccaratGameManager>(
                FindObjectsInactive.Include
            );

        if (baccaratCanvas == null)
            baccaratCanvas = FindObjectByName("BaccaratCanvas");

        if (baccaratPanel == null)
            baccaratPanel = FindObjectByName("BaccaratPanel");

        if (tableCollider == null)
        {
            Debug.LogError(
                "[Baccarat] Thiếu Collider2D trên object bàn Baccarat!",
                this
            );
        }

        if (transform.IsChildOf(baccaratCanvas != null
                ? baccaratCanvas.transform
                : null))
        {
            Debug.LogError(
                "[Baccarat] Object nhận nhấn chuột không được nằm trong BaccaratCanvas!",
                this
            );
        }
    }

    private GameObject FindObjectByName(string objectName)
    {
        Transform[] allObjects = Object.FindObjectsByType<Transform>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );

        foreach (Transform item in allObjects)
        {
            if (item.name == objectName &&
                item.gameObject.scene == gameObject.scene)
            {
                return item.gameObject;
            }
        }

        return null;
    }

    private void OnMouseDown()
    {
        if (tableCollider == null || !tableCollider.enabled)
            return;

        if (gameManager == null)
        {
            Debug.LogError(
                "[Baccarat] Không tìm thấy BaccaratGameManager!",
                this
            );
            return;
        }

        OpenBaccarat();
    }

    public void OpenBaccarat()
    {
        if (gameManager == null)
        {
            Debug.LogError("[Baccarat] GameManager chưa được gán!", this);
            return;
        }

        if (baccaratCanvas != null)
            baccaratCanvas.SetActive(true);

        if (baccaratPanel != null)
            baccaratPanel.SetActive(true);

        gameManager.OpenBaccarat();
    }

    public void CloseBaccarat()
    {
        if (gameManager != null)
            gameManager.CloseBaccarat();
    }
}