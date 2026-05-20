using UnityEngine;

/// <summary>
/// Toggles the GameObject's Y scale between +1 and -1 to flip the sprite
/// vertically when transitioning between floor and ceiling.
/// </summary>
public class SpriteFlip : MonoBehaviour
{
    private const float FlippedScale   = -.2f;
    private const float UnflippedScale =  .2f;

    private bool _isFlipped;

    /// <summary>
    /// Flips the Y scale. Call each time the character transitions
    /// between floor and ceiling contact.
    /// </summary>
    public void Flip()
    {
        _isFlipped = !_isFlipped;
        Vector3 scale = transform.localScale;
        scale.y = _isFlipped ? FlippedScale : UnflippedScale;
        transform.localScale = scale;
        Debug.Log($"[SpriteFlip] Flip — isFlipped={_isFlipped}, localScale.y={scale.y}");
    }
}
