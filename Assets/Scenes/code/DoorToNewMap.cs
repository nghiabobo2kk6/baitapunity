
using UnityEngine;
using UnityEngine.SceneManagement;

public class DoorToNewMap : MonoBehaviour
{
    [Header("Scene Settings")]
    [SerializeField] private string nextSceneName = "Bargtok";

    [Header("Door Sprites")]
    [SerializeField] private SpriteRenderer doorRenderer;
    [SerializeField] private Sprite closedDoorSprite;
    [SerializeField] private Sprite openDoorSprite;

    private bool isOpen = false;
    private bool isLoading = false;

    private void Awake()
    {
        if (doorRenderer == null)
        {
            doorRenderer = GetComponent<SpriteRenderer>();
        }

        isOpen = false;
        isLoading = false;

        UpdateDoorSprite();
    }

    private void OnMouseDown()
    {
        Debug.Log("PalaceDoor: Mouse clicked.");

        if (isLoading)
            return;

        if (!isOpen)
        {
            isOpen = true;
            UpdateDoorSprite();

            Debug.Log("PalaceDoor: Opened. Click again to travel.");
            return;
        }

        LoadNextScene();
    }

    private void LoadNextScene()
    {
        if (string.IsNullOrWhiteSpace(nextSceneName))
        {
            Debug.LogError("DoorToNewMap: Next Scene Name is empty!");
            return;
        }

        if (!Application.CanStreamedLevelBeLoaded(nextSceneName))
        {
            Debug.LogError(
                "DoorToNewMap: Cannot load Scene '" + nextSceneName +
                "'. Check the exact Scene name and Build Profiles > Scene List."
            );
            return;
        }

        Debug.Log("DoorToNewMap: Loading " + nextSceneName);

        isLoading = true;
        SceneManager.LoadScene(nextSceneName);
    }

    private void UpdateDoorSprite()
    {
        if (doorRenderer == null)
            return;

        Sprite spriteToUse = isOpen
            ? openDoorSprite
            : closedDoorSprite;

        if (spriteToUse != null)
        {
            doorRenderer.sprite = spriteToUse;
        }
    }
}
