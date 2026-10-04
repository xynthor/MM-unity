using UnityEngine;

[DisallowMultipleComponent]
public class MMCombatActor : MonoBehaviour
{
    public MMHealth health;
    public Animator animator;
    public bool disableMovementOnDeath = true;
    public bool disableCombatOnDeath = true;
    public bool disableCharacterControllerOnDeath;

    MMThirdPersonController movement;
    MMHeroCombatController combat;

#if UNITY_EDITOR
    public int EditorHitReactionCount { get; private set; }
    public int EditorDeathReactionCount { get; private set; }
#endif

    static readonly int HitHash = Animator.StringToHash("Hit");
    static readonly int DeathHash = Animator.StringToHash("Death");

    void Awake()
    {
        if (!health) health = GetComponent<MMHealth>();
        if (!animator) animator = GetComponentInChildren<Animator>();

        movement = GetComponent<MMThirdPersonController>();
        combat = GetComponent<MMHeroCombatController>();

        if (health)
        {
            health.Damaged += OnDamaged;
            health.Died += OnDied;
        }
    }

    void OnDestroy()
    {
        if (health)
        {
            health.Damaged -= OnDamaged;
            health.Died -= OnDied;
        }
    }

    void OnDamaged(MMHealth value, float amount, GameObject source)
    {
        if (value.IsDead)
            return;

        if (CanTrigger(HitHash))
            animator.SetTrigger(HitHash);

#if UNITY_EDITOR
        EditorHitReactionCount++;
#endif
    }

    void OnDied(MMHealth value, GameObject source)
    {
        if (CanTrigger(DeathHash))
            animator.SetTrigger(DeathHash);

        if (disableMovementOnDeath && movement)
            movement.enabled = false;

        if (disableCombatOnDeath && combat)
        {
            combat.ResetCombat();
            combat.enabled = false;
        }

        if (disableCharacterControllerOnDeath)
        {
            CharacterController cc = GetComponent<CharacterController>();
            if (cc) cc.enabled = false;
        }

#if UNITY_EDITOR
        EditorDeathReactionCount++;
#endif
    }

    bool CanTrigger(int hash)
    {
        if (!animator ||
            !animator.runtimeAnimatorController ||
            !animator.isActiveAndEnabled)
            return false;

        foreach (AnimatorControllerParameter parameter in animator.parameters)
            if (parameter.nameHash == hash)
                return true;

        return false;
    }
}

public static class MMHeroVitalsBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void EnsureVitals()
    {
        MMThirdPersonController player = null;
        MMThirdPersonController fallback = null;

        foreach (MMThirdPersonController candidate in
                 Object.FindObjectsByType<MMThirdPersonController>(
                     FindObjectsInactive.Exclude))
        {
            if (!candidate || !candidate.gameObject.activeInHierarchy)
                continue;

            if (!fallback)
                fallback = candidate;

            if (candidate.transform.parent &&
                candidate.transform.parent.name.Contains("Persistent Player"))
            {
                player = candidate;
                break;
            }
        }

        if (!player)
            player = fallback;

        if (!player)
            return;

        GameObject go = player.gameObject;

        MMHealth health = go.GetComponent<MMHealth>();
        if (!health)
        {
            health = go.AddComponent<MMHealth>();
            health.maxHealth = 100f;
            health.destroyOnDeath = false;
            health.ResetHealth();
        }

        MMCombatActor actor = go.GetComponent<MMCombatActor>();
        if (!actor)
            actor = go.AddComponent<MMCombatActor>();

        actor.health = health;
        actor.animator = player.animator;
        actor.disableMovementOnDeath = true;
        actor.disableCombatOnDeath = true;
        actor.disableCharacterControllerOnDeath = false;
    }
}
