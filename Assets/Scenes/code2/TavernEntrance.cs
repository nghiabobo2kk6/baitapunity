
using UnityEngine;
using UnityEngine.SceneManagement;

public class TavernEntrance : MonoBehaviour
{
    [Header("Tavern Scene")]
    [SerializeField] private string sceneName = "SaloonInterior";

    private bool isLoading = false;

    private void OnMouseDown()
    {
        EnterTavern();
    }

    public void EnterTavern()
    {
        if (isLoading)
            return;

        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogError(
                "Cannot load scene: " + sceneName +
                ". Please add SaloonInterior to Build Profiles."
            );
            return;
        }

        isLoading = true;

        Debug.Log("Loading tavern scene: " + sceneName);

        SceneManager.LoadScene(sceneName);
    }
}
