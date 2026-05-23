using UnityEngine;

namespace Crystal.HeroEnemy
{
    [RequireComponent(typeof(Collider2D))]
    public sealed class HeroHealingPickup : MonoBehaviour
    {
        [Header("Healing")]
        [SerializeField] private HealingItemDefinition itemDefinition;
        [SerializeField] private float healAmountOverride = -1f;

        [Header("Consumption")]
        [SerializeField] private bool deactivateOnConsume = true;
        [SerializeField] private bool destroyOnConsume;
        [SerializeField] private bool resetAvailabilityOnEnable = true;

        [Header("Collider")]
        [SerializeField] private bool forceTriggerCollider = true;

        private bool consumed;

        public HealingItemDefinition ItemDefinition => itemDefinition;
        public bool IsAvailable => isActiveAndEnabled && !consumed && HealAmount > 0f;
        public float HealAmount => healAmountOverride >= 0f ? healAmountOverride : itemDefinition != null ? itemDefinition.HealAmount : 0f;

        public bool Matches(HealingItemDefinition requiredDefinition)
        {
            return requiredDefinition == null || itemDefinition == requiredDefinition;
        }

        public bool TryConsume(CombatHealth target)
        {
            if (!IsAvailable || target == null || !target.IsAlive)
                return false;

            target.Heal(HealAmount);
            consumed = true;

            if (destroyOnConsume)
                Destroy(gameObject);
            else if (deactivateOnConsume)
                gameObject.SetActive(false);

            return true;
        }

        private void OnEnable()
        {
            if (resetAvailabilityOnEnable)
                consumed = false;
        }

        private void OnValidate()
        {
            healAmountOverride = Mathf.Max(-1f, healAmountOverride);

            if (!forceTriggerCollider)
                return;

            Collider2D pickupCollider = GetComponent<Collider2D>();
            if (pickupCollider != null)
                pickupCollider.isTrigger = true;
        }
    }
}
