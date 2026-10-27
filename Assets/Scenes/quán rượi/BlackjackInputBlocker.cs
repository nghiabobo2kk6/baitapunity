
using UnityEngine;

public class BlackjackInputBlocker : MonoBehaviour
{
    [Header("Other Table Colliders")]
    [SerializeField] private Collider2D[] otherTableColliders;

    private bool[] originalStates;
    private bool initialized;

    private void Awake()
    {
        if (otherTableColliders == null)
            otherTableColliders = new Collider2D[0];

        originalStates = new bool[otherTableColliders.Length];

        for (int i = 0; i < otherTableColliders.Length; i++)
        {
            if (otherTableColliders[i] != null)
            {
                originalStates[i] = otherTableColliders[i].enabled;
            }
        }

        initialized = true;
    }

    private void OnEnable()
    {
        SetOtherTablesEnabled(false);
    }

    private void OnDisable()
    {
        SetOtherTablesEnabled(true);
    }

    private void SetOtherTablesEnabled(bool restoreOriginal)
    {
        if (!initialized || otherTableColliders == null)
            return;

        for (int i = 0; i < otherTableColliders.Length; i++)
        {
            Collider2D tableCollider = otherTableColliders[i];

            if (tableCollider == null)
                continue;

            if (restoreOriginal)
            {
                if (originalStates != null &&
                    i < originalStates.Length)
                {
                    tableCollider.enabled = originalStates[i];
                }
            }
            else
            {
                tableCollider.enabled = false;
            }
        }
    }
}
