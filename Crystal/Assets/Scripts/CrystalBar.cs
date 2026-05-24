using UnityEngine;
using UnityEngine.UI;

namespace Crystal
{
    /// <summary>
    /// Drives the crystal charge bar in the header.
    /// Uses anchorMax.x on the fill RectTransform to represent the charge percent.
    /// The fill color lerps from red (0%) to white (100%).
    /// The crystal shine SpriteRenderer alpha lerps from 0 (0%) to 1 (100%).
    /// </summary>
    public sealed class CrystalBar : MonoBehaviour
    {
        /// <summary>The RectTransform of the fill — anchorMax.x is driven to represent the charge percent.</summary>
        [SerializeField] private RectTransform fillRect;
        [SerializeField] private Image fillImage;
        [SerializeField] private SpriteRenderer crystalShine;

        [SerializeField, Range(0f, 1f)] private float fillPercent = 1f;

        private static readonly Color ColorEmpty = Color.red;
        private static readonly Color ColorFull  = Color.white;

        private void OnValidate()
        {
            if (fillRect != null && fillImage != null)
                SetFill(fillPercent);
        }

        /// <summary>
        /// Sets the fill bar to the given percentage [0, 1].
        /// </summary>
        public void SetFill(float percent)
        {
            percent = Mathf.Clamp01(percent);
            fillPercent = percent;

            fillRect.anchorMax = new Vector2(percent, fillRect.anchorMax.y);
            fillImage.color = Color.Lerp(ColorEmpty, ColorFull, percent);

            if (crystalShine != null)
            {
                Color shineColor = crystalShine.color;
                shineColor.a = percent;
                crystalShine.color = shineColor;
            }
        }
    }
}
