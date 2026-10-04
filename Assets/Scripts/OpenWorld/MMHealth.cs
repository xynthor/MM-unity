using System;
using UnityEngine;

[DisallowMultipleComponent]
public class MMHealth : MonoBehaviour
{
    [Min(1f)] public float maxHealth = 100f;
    public bool destroyOnDeath;

    [SerializeField] float currentHealth;

    public float CurrentHealth => currentHealth;
    public float NormalizedHealth => maxHealth > 0f ? currentHealth / maxHealth : 0f;
    public bool IsDead => currentHealth <= 0f;
    public int DamageEventCount { get; private set; }

    public event Action<MMHealth, float, GameObject> Damaged;
    public event Action<MMHealth, GameObject> Died;

    void Awake()
    {
        ResetHealth();
    }

    public void ResetHealth()
    {
        currentHealth = Mathf.Max(1f, maxHealth);
        DamageEventCount = 0;
    }

    public bool ApplyDamage(float amount, GameObject source = null)
    {
        if (IsDead || amount <= 0f)
            return false;

        float applied = Mathf.Min(amount, currentHealth);
        currentHealth -= applied;
        DamageEventCount++;
        Damaged?.Invoke(this, applied, source);

        if (currentHealth <= 0f)
        {
            currentHealth = 0f;
            Died?.Invoke(this, source);

            if (destroyOnDeath)
                Destroy(gameObject);
        }

        return true;
    }

    public void Heal(float amount)
    {
        if (IsDead || amount <= 0f)
            return;

        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
    }
}
