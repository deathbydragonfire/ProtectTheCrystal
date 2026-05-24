using TMPro;
using UnityEngine;

namespace Crystal.HeroEnemy
{
    /// <summary>
    /// Displays the hero's current AI decision above its health bar.
    /// Attach to a TextMeshPro object that is a child of the HealthBarCanvas.
    /// </summary>
    public sealed class HeroDecisionDebugText : MonoBehaviour
    {
        [SerializeField] private HeroEnemyBrain brain;
        [SerializeField] private TextMeshProUGUI label;

        private void Awake()
        {
            if (label == null)
                label = GetComponent<TextMeshProUGUI>();
        }

        private void Update()
        {
            if (brain == null || label == null)
                return;

            label.text = brain.CurrentDecision.ToString();
        }
    }
}
