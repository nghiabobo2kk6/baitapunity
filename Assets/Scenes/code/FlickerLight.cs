
using UnityEngine;
using System.Collections;

public class FireFlicker : MonoBehaviour
{
    [Header("9 Fire Sprite Renderers")]
    public SpriteRenderer[] fires = new SpriteRenderer[9];

    [Header("Flicker Settings")]
    [Min(0.01f)] public float minDelay = 0.05f;
    [Min(0.01f)] public float maxDelay = 0.3f;

    private void Start()
    {
        // Find child SpriteRenderers if the array was not assigned.
        bool hasAssignedFire = false;

        foreach (SpriteRenderer fire in fires)
        {
            if (fire != null)
            {
                hasAssignedFire = true;
                break;
            }
        }

        if (!hasAssignedFire)
        {
            SpriteRenderer[] found =
                GetComponentsInChildren<SpriteRenderer>(true);

            fires = new SpriteRenderer[Mathf.Min(found.Length, 9)];

            for (int i = 0; i < fires.Length; i++)
                fires[i] = found[i];
        }

        maxDelay = Mathf.Max(maxDelay, minDelay);

        foreach (SpriteRenderer fire in fires)
        {
            if (fire != null)
            {
                fire.enabled = true;
                StartCoroutine(FlickerFire(fire));
            }
            else
            {
                Debug.LogWarning(
                    "FireFlicker: Co o SpriteRenderer chua duoc gan!",
                    this
                );
            }
        }
    }

    private IEnumerator FlickerFire(SpriteRenderer fire)
    {
        while (true)
        {
            yield return new WaitForSeconds(
                Random.Range(minDelay, maxDelay)
            );

            if (fire != null)
                fire.enabled = !fire.enabled;
            else
                yield break;
        }
    }
}
