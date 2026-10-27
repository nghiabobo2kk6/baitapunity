
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using System.Collections;

[RequireComponent(typeof(Collider2D))]
public class PalaceDoorController : MonoBehaviour
{
    [Header("Door Animation")]
    public Animator doorAnimator;
    public string openStateName = "DoorOpen";

    [Header("Scene Transition")]
    public string nextSceneName = "Bargtok";

    [Header("Mouse Input")]
    public Camera targetCamera;

    private Collider2D doorCollider;
    private bool isOpen;
    private bool isLoadingScene;

    private void Awake()
    {
        doorCollider = GetComponent<Collider2D>();

        if (doorAnimator == null)
            doorAnimator = GetComponent<Animator>();

        if (targetCamera == null)
            targetCamera = Camera.main;

        if (doorCollider != null)
            doorCollider.enabled = true;
    }

    private void Update()
    {
        if (isLoadingScene || !Input.GetMouseButtonDown(0))
            return;

        if (EventSystem.current != null &&
            EventSystem.current.IsPointerOverGameObject())
            return;

        if (targetCamera == null)
            targetCamera = Camera.main;

        if (targetCamera == null || doorCollider == null)
            return;

        Vector2 mouseWorldPosition =
            targetCamera.ScreenToWorldPoint(Input.mousePosition);

        if (!doorCollider.OverlapPoint(mouseWorldPosition))
            return;

        Debug.Log("Palace door clicked.", this);

        if (!isOpen)
        {
            OpenDoor();
        }
        else
        {
            StartCoroutine(LoadNextScene());
        }
    }

    private void OpenDoor()
    {
        if (doorAnimator == null)
        {
            Debug.LogError("Door Animator is not assigned!", this);
            return;
        }

        int stateHash = Animator.StringToHash(openStateName);

        if (!doorAnimator.HasState(0, stateHash))
        {
            Debug.LogError(
                "Animator state not found: " + openStateName, this);
            return;
        }

        isOpen = true;
        doorAnimator.Play(stateHash, 0, 0f);
    }

    private IEnumerator LoadNextScene()
    {
        isLoadingScene = true;

        if (string.IsNullOrWhiteSpace(nextSceneName))
        {
            Debug.LogError("Next Scene Name is empty!", this);
            isLoadingScene = false;
            yield break;
        }

        if (!Application.CanStreamedLevelBeLoaded(nextSceneName))
        {
            Debug.LogError(
                "Cannot load scene '" + nextSceneName +
                "'. Add it to Build Profiles > Scene List.", this);
            isLoadingScene = false;
            yield break;
        }

        Debug.Log("Loading scene: " + nextSceneName, this);

        yield return SceneManager.LoadSceneAsync(nextSceneName);
    }
}
