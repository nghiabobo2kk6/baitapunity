
using UnityEngine;

public class KingPersistence : MonoBehaviour
{
    private static KingPersistence instance;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
    }
}
