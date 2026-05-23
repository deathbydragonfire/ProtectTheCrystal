using System;
using UnityEngine;

namespace Crystal.HeroEnemy
{
    public sealed class CombatHealth : MonoBehaviour, IDamageable
    {
        [Header("Health")]
        [SerializeField] private float maxHealth = 100f;
        [SerializeField] private bool startAtMaxHealth = true;
        [SerializeField] private float startingHealth = 100f;
        [SerializeField] private bool invulnerable;
        [SerializeField] private bool disableGameObjectOnDeath;

        private float currentHealth;
        private bool initialized;
        private bool died;

        public event Action<float, float> HealthChanged;
        public event Action<DamageInfo> Damaged;
        public event Action<float> Healed;
        public event Action<CombatHealth> Died;

        public float MaxHealth => maxHealth;

        public float CurrentHealth
        {
            get
            {
                EnsureInitialized();
                return currentHealth;
            }
        }

        public float HealthPercent => maxHealth <= 0f ? 0f : CurrentHealth / maxHealth;

        public bool IsAlive
        {
            get
            {
                EnsureInitialized();
                return !died && currentHealth > 0f;
            }
        }

        public void ApplyDamage(DamageInfo damageInfo)
        {
            EnsureInitialized();

            if (died || invulnerable || damageInfo.Amount <= 0f)
                return;

            float previousHealth = currentHealth;
            currentHealth = Mathf.Max(0f, currentHealth - damageInfo.Amount);

            if (!Mathf.Approximately(previousHealth, currentHealth))
            {
                HealthChanged?.Invoke(currentHealth, maxHealth);
                Damaged?.Invoke(damageInfo);
            }

            if (currentHealth <= 0f)
                Die();
        }

        public void Heal(float amount)
        {
            EnsureInitialized();

            if (died || amount <= 0f)
                return;

            float previousHealth = currentHealth;
            currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
            float healedAmount = currentHealth - previousHealth;

            if (healedAmount <= 0f)
                return;

            HealthChanged?.Invoke(currentHealth, maxHealth);
            Healed?.Invoke(healedAmount);
        }

        public void ResetHealth()
        {
            initialized = true;
            died = false;
            currentHealth = Mathf.Clamp(startAtMaxHealth ? maxHealth : startingHealth, 0f, maxHealth);
            HealthChanged?.Invoke(currentHealth, maxHealth);
        }

        public void SetInvulnerable(bool value)
        {
            invulnerable = value;
        }

        private void Awake()
        {
            ResetHealth();
        }

        private void Die()
        {
            if (died)
                return;

            died = true;
            Died?.Invoke(this);

            if (disableGameObjectOnDeath)
                gameObject.SetActive(false);
        }

        private void EnsureInitialized()
        {
            if (!initialized)
                ResetHealth();
        }

        private void OnValidate()
        {
            maxHealth = Mathf.Max(1f, maxHealth);
            startingHealth = Mathf.Clamp(startingHealth, 0f, maxHealth);
        }
    }
}
