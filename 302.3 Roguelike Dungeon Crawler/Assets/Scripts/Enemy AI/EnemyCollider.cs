using UnityEngine;

[ExecuteAlways]
public class EnemyCollider : MonoBehaviour
{
    public CapsuleCollider2D capsule;
    public SpriteRenderer spriteRenderer;
    [Tooltip("Align once on Awake/Start")]
    public bool alignOnStart = true;
    [Tooltip("If true, align every frame (use for debugging)")]
    public bool alignContinuously = false;
    [Tooltip("Optionally adjust capsule size multiplier")]
    public Vector2 sizeMultiplier = Vector2.one;

    Vector2 initialSize;
    Vector2 initialOffset;

    void Awake()
    {
        if (capsule == null) capsule = GetComponent<CapsuleCollider2D>();
        if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (capsule != null)
        {
            initialSize = capsule.size;
            initialOffset = capsule.offset;
        }

        if (alignOnStart) AlignColliderToSprite();
    }

    void LateUpdate()
    {
        if (alignContinuously) AlignColliderToSprite();
    }

    public void AlignColliderToSprite()
    {
        if (capsule == null || spriteRenderer == null) return;

        // sprite bounds center in world, convert to local space of the capsule's transform
        Vector3 spriteWorldCenter = spriteRenderer.bounds.center;
        Vector3 localCenter = capsule.transform.InverseTransformPoint(spriteWorldCenter);

        // set capsule offset (use only x,y)
        capsule.offset = new Vector2(localCenter.x, localCenter.y);

        // optionally set capsule size to sprite bounds (tweak with multiplier)
        Vector2 spriteSizeLocal = new Vector2(spriteRenderer.bounds.size.x, spriteRenderer.bounds.size.y);
        capsule.size = new Vector2(spriteSizeLocal.x * sizeMultiplier.x, spriteSizeLocal.y * sizeMultiplier.y);
    }
}
