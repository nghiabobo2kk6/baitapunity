
using UnityEngine;

public class SicBoInputBlocker : MonoBehaviour
{
    [Header("Colliders of other tables to disable")]
    [SerializeField] private Collider2D[] otherTableColliders;

    private bool[] previousStates;

    private void Awake()
    {
        previousStates = new bool[otherTableColliders.Length];

        for (int i = 0; i < otherTableColliders.Length; i++)
        {
            if (otherTableColliders[i] != null)
                previousStates[i] = otherTableColliders[i].enabled;
        }
    }

    private void OnEnable()
    {
        if (otherTableColliders == null)
            return;

        for (int i = 0; i < otherTableColliders.Length; i++)
        {
            if (otherTableColliders[i] != null)
                otherTableColliders[i].enabled = false;
        }
    }

    private void OnDisable()
    {
        if (otherTableColliders == null || previousStates == null)
            return;

        for (int i = 0; i < otherTableColliders.Length; i++)
        {
            if (otherTableColliders[i] != null)
                otherTableColliders[i].enabled = previousStates[i];
        }
    }
}

