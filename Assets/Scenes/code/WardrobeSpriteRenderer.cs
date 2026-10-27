
using UnityEngine;
using System.Collections;
using UnityEngine.EventSystems;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Collider2D))]
public class WardrobeSpriteRenderer : MonoBehaviour
{
    [Header("Wardrobe Sprite")]
    [SerializeField] private Sprite wardrobeSprite;

    [Header("Characters")]
    [SerializeField] private GameObject king;
    [SerializeField] private GameObject kingCivilian;

    [Header("Dust Prefab")]
    [SerializeField] private GameObject dustEffectPrefab;
    [SerializeField] private float dustLifetime = 0.5f;
    [SerializeField] private Vector3 dustOffset = Vector3.zero;

    [Header("Mouse Input")]
    [SerializeField] private Camera targetCamera;

    private SpriteRenderer spriteRenderer;
    private Collider2D wardrobeCollider;

    private Behaviour kingControl;
    private Behaviour civilianControl;

    private bool isCivilian;
    private bool isTransforming;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        wardrobeCollider = GetComponent<Collider2D>();

        UpdateWardrobeSprite();

        if (targetCamera == null)
            targetCamera = Camera.main;

        if (king != null)
            kingControl = FindControl(king, "KingMovement");

        if (kingCivilian != null)
            civilianControl = FindControl(
                kingCivilian, "KingCivilianMovement");

        isCivilian = false;

        if (king != null)
            king.SetActive(true);

        if (kingCivilian != null)
            kingCivilian.SetActive(false);

        SetControl(kingControl, true);
        SetControl(civilianControl, false);

        if (wardrobeCollider != null)
            wardrobeCollider.enabled = true;
    }

    private void OnValidate()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        UpdateWardrobeSprite();
    }

    private void UpdateWardrobeSprite()
    {
        if (spriteRenderer != null && wardrobeSprite != null)
        {
            spriteRenderer.sprite = wardrobeSprite;
            spriteRenderer.enabled = true;
        }
    }

    private Behaviour FindControl(GameObject character, string typeName)
    {
        if (character == null)
            return null;

        MonoBehaviour[] scripts =
            character.GetComponents<MonoBehaviour>();

        foreach (MonoBehaviour script in scripts)
        {
            if (script != null && script.GetType().Name == typeName)
                return script;
        }

        return null;
    }

    private void SetControl(Behaviour control, bool state)
    {
        if (control != null)
            control.enabled = state;
    }

    private void Update()
    {
        if (isTransforming || !Input.GetMouseButtonDown(0))
            return;

        if (EventSystem.current != null &&
            EventSystem.current.IsPointerOverGameObject())
            return;

        if (targetCamera == null)
            targetCamera = Camera.main;

        if (targetCamera == null || wardrobeCollider == null)
            return;

        Vector2 mouseWorldPosition =
            targetCamera.ScreenToWorldPoint(Input.mousePosition);

        if (!wardrobeCollider.OverlapPoint(mouseWorldPosition))
            return;

        Debug.Log("Wardrobe clicked", this);

        if (king == null || kingCivilian == null)
        {
            Debug.LogError(
                "Assign both King and King Civilian in the Inspector.",
                this);
            return;
        }

        StartCoroutine(TransformCharacter());
    }

    private IEnumerator TransformCharacter()
    {
        isTransforming = true;

        GameObject oldCharacter = isCivilian ? kingCivilian : king;
        GameObject newCharacter = isCivilian ? king : kingCivilian;

        Behaviour oldControl = isCivilian ? civilianControl : kingControl;
        Behaviour newControl = isCivilian ? kingControl : civilianControl;

        Vector3 position = oldCharacter.transform.position;
        Quaternion rotation = oldCharacter.transform.rotation;

        SetControl(oldControl, false);

        if (dustEffectPrefab != null)
        {
            GameObject dust = Instantiate(
                dustEffectPrefab,
                position + dustOffset,
                Quaternion.identity);

            Animator dustAnimator = dust.GetComponent<Animator>();

            if (dustAnimator != null)
            {
                dustAnimator.Play(0, 0, 0f);
                dustAnimator.Update(0f);
            }

            Destroy(dust, dustLifetime);
        }

        oldCharacter.SetActive(false);

        newCharacter.transform.position = position;
        newCharacter.transform.rotation = rotation;
        newCharacter.SetActive(true);

        SetControl(newControl, true);

        isCivilian = !isCivilian;

        yield return null;

        isTransforming = false;
    }
}
