using UnityEngine;

namespace Crystal.HeroEnemy
{
    [CreateAssetMenu(fileName = "HealingItemDefinition", menuName = "Crystal/Hero Enemy/Healing Item Definition")]
    public sealed class HealingItemDefinition : ScriptableObject
    {
        [SerializeField] private string displayName = "Healing Item";
        [SerializeField] private float healAmount = 35f;

        public string DisplayName => displayName;
        public float HealAmount => healAmount;

        private void OnValidate()
        {
            healAmount = Mathf.Max(0f, healAmount);
        }
    }
}
