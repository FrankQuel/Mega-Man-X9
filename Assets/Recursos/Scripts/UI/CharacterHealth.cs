using System;
using UnityEngine;

namespace MegaManX9.UI
{
    /// <summary>Health state independent from preview animation and HUD drawing.</summary>
    [DisallowMultipleComponent]
    public sealed class CharacterHealth : MonoBehaviour
    {
        [SerializeField, Min(1)] private int maxHealth = 28;
        [SerializeField, Min(0)] private int currentHealth = 28;
        public int MaxHealth => maxHealth;
        public int CurrentHealth => currentHealth;
        public float Normalized => maxHealth > 0 ? Mathf.Clamp01(currentHealth / (float)maxHealth) : 0;
        public bool IsAlive => currentHealth > 0;
        public event Action<CharacterHealth> Changed;

        private void Awake() => ClampValues();
        private void OnValidate() => ClampValues();
        private void ClampValues()
        {
            maxHealth = Mathf.Max(1, maxHealth);
            currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        }

        public void Configure(int maximum = 28, bool resetToFull = true)
        {
            int previousMaximum = maxHealth, previousValue = currentHealth;
            maxHealth = Mathf.Max(1, maximum);
            currentHealth = resetToFull ? maxHealth : Mathf.Clamp(currentHealth, 0, maxHealth);
            if (previousMaximum != maxHealth || previousValue != currentHealth) Changed?.Invoke(this);
        }

        /// <returns>Damage actually applied, clamped to remaining health.</returns>
        public int ApplyDamage(int amount)
        {
            if (amount <= 0 || currentHealth <= 0) return 0;
            int applied = Math.Min(amount, currentHealth);
            currentHealth -= applied; Changed?.Invoke(this); return applied;
        }

        /// <returns>Health actually restored, clamped to the maximum without integer overflow.</returns>
        public int Heal(int amount)
        {
            if (amount <= 0 || currentHealth >= maxHealth) return 0;
            int restored = Math.Min(amount, maxHealth - currentHealth);
            currentHealth += restored; Changed?.Invoke(this); return restored;
        }

        public void ResetHealth()
        {
            if (currentHealth == maxHealth) return;
            currentHealth = maxHealth; Changed?.Invoke(this);
        }

        // Public alias also supports Unity's Reset command on a newly attached component.
        public void Reset() => ResetHealth();
    }
}
