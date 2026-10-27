
using UnityEngine;
using UnityEngine.SceneManagement;

public class ExitSaloonDoor : MonoBehaviour
{
    private void OnMouseDown()
    {
        SceneManager.LoadScene("Bargtok");
    }
}
