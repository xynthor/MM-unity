using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MMHeroCombatController : MonoBehaviour
{
    [Header("Animation")]
    public Animator animator;
    public float comboReset = 0.85f;

    [Header("Melee Hit Detection")]
    public Transform hitOrigin;
    public LayerMask damageMask = ~0;
    public float hitHeight = 0.72f;

    [Header("Light Combo")]
    public float lightDamage = 18f;
    public float lightReach = 1.15f;
    public float lightRadius = 0.55f;
    public float lightHitDelay = 0.16f;

    [Header("Heavy")]
    public float heavyDamage = 32f;
    public float heavyReach = 1.35f;
    public float heavyRadius = 0.65f;
    public float heavyHitDelay = 0.26f;

    [Header("Kick")]
    public float kickDamage = 14f;
    public float kickReach = 0.95f;
    public float kickRadius = 0.45f;
    public float kickHitDelay = 0.14f;

    int comboStep;
    float lastAttackTime;
    readonly Collider[] hitBuffer = new Collider[32];

#if UNITY_EDITOR
    int editorMeleeSwingCount;
    int editorMeleeTargetHitCount;
#endif

    static readonly int Attack1Hash = Animator.StringToHash("Attack1");
    static readonly int Attack2Hash = Animator.StringToHash("Attack2");
    static readonly int Attack3Hash = Animator.StringToHash("Attack3");
    static readonly int HeavyHash = Animator.StringToHash("HeavyAttack");
    static readonly int KickHash = Animator.StringToHash("Kick");
    static readonly int BlockHash = Animator.StringToHash("Block");
    static readonly int DodgeFHash = Animator.StringToHash("DodgeForward");
    static readonly int DodgeBHash = Animator.StringToHash("DodgeBackward");
    static readonly int DodgeLHash = Animator.StringToHash("DodgeLeft");
    static readonly int DodgeRHash = Animator.StringToHash("DodgeRight");
    static readonly int HitHash = Animator.StringToHash("Hit");
    static readonly int DeathHash = Animator.StringToHash("Death");
    static readonly int CastHash = Animator.StringToHash("Cast");

    void Awake()
    {
        if (!animator)
            animator = GetComponentInChildren<Animator>();
    }

    void Update()
    {
        if (!animator ||
            !animator.runtimeAnimatorController ||
            !animator.isActiveAndEnabled)
            return;

        if (Time.time - lastAttackTime > comboReset)
            comboStep = 0;

        if (Input.GetMouseButtonDown(0))
            TriggerLightAttack();

        animator.SetBool(BlockHash, Input.GetMouseButton(1));

        if (Input.GetKeyDown(KeyCode.F))
            TriggerHeavyAttack();

        if (Input.GetKeyDown(KeyCode.E))
            TriggerKick();

        if (Input.GetKeyDown(KeyCode.R))
            animator.SetTrigger(CastHash);

        if (Input.GetKeyDown(KeyCode.Q))
        {
            float h = Input.GetAxisRaw("Horizontal");
            float v = Input.GetAxisRaw("Vertical");

            if (Mathf.Abs(h) > Mathf.Abs(v))
                animator.SetTrigger(h < 0f ? DodgeLHash : DodgeRHash);
            else if (v < -0.1f)
                animator.SetTrigger(DodgeBHash);
            else
                animator.SetTrigger(DodgeFHash);
        }
    }

    void TriggerLightAttack()
    {
        comboStep = (comboStep % 3) + 1;
        lastAttackTime = Time.time;

        animator.SetTrigger(
            comboStep == 1 ? Attack1Hash :
            comboStep == 2 ? Attack2Hash :
            Attack3Hash);

        StartCoroutine(DelayedMeleeHit(
            lightHitDelay,
            lightDamage,
            lightReach,
            lightRadius));
    }

    void TriggerHeavyAttack()
    {
        lastAttackTime = Time.time;
        animator.SetTrigger(HeavyHash);
        StartCoroutine(DelayedMeleeHit(
            heavyHitDelay,
            heavyDamage,
            heavyReach,
            heavyRadius));
    }

    void TriggerKick()
    {
        lastAttackTime = Time.time;
        animator.SetTrigger(KickHash);
        StartCoroutine(DelayedMeleeHit(
            kickHitDelay,
            kickDamage,
            kickReach,
            kickRadius));
    }

    IEnumerator DelayedMeleeHit(
        float delay,
        float damage,
        float reach,
        float radius)
    {
        if (delay > 0f)
            yield return new WaitForSeconds(delay);

        ApplyMeleeHit(damage, reach, radius);
    }

    int ApplyMeleeHit(float damage, float reach, float radius)
    {
        Vector3 origin = hitOrigin
            ? hitOrigin.position
            : transform.position + Vector3.up * hitHeight;

        Vector3 center = origin + transform.forward * Mathf.Max(0f, reach);
        int count = Physics.OverlapSphereNonAlloc(
            center,
            Mathf.Max(0.05f, radius),
            hitBuffer,
            damageMask,
            QueryTriggerInteraction.Ignore);

        var damaged = new HashSet<MMHealth>();
        int successfulHits = 0;

        for (int i = 0; i < count; i++)
        {
            Collider collider = hitBuffer[i];
            if (!collider)
                continue;

            MMHealth health = collider.GetComponentInParent<MMHealth>();
            if (!health || health.IsDead || damaged.Contains(health))
                continue;

            Transform target = health.transform;
            if (target == transform ||
                target.IsChildOf(transform) ||
                transform.IsChildOf(target))
                continue;

            damaged.Add(health);
            if (health.ApplyDamage(damage, gameObject))
                successfulHits++;
        }

#if UNITY_EDITOR
        editorMeleeSwingCount++;
        editorMeleeTargetHitCount += successfulHits;
#endif

        return successfulHits;
    }

    public void PlayHit()
    {
        if (animator &&
            animator.runtimeAnimatorController &&
            animator.isActiveAndEnabled)
            animator.SetTrigger(HitHash);
    }

    public void PlayDeath()
    {
        if (animator &&
            animator.runtimeAnimatorController &&
            animator.isActiveAndEnabled)
            animator.SetTrigger(DeathHash);
    }

    public void ResetCombat()
    {
        comboStep = 0;
        StopAllCoroutines();

        if (!animator ||
            !animator.runtimeAnimatorController ||
            !animator.isActiveAndEnabled)
            return;

        animator.ResetTrigger(Attack1Hash);
        animator.ResetTrigger(Attack2Hash);
        animator.ResetTrigger(Attack3Hash);
        animator.ResetTrigger(HeavyHash);
        animator.ResetTrigger(KickHash);
        animator.ResetTrigger(DodgeFHash);
        animator.ResetTrigger(DodgeBHash);
        animator.ResetTrigger(DodgeLHash);
        animator.ResetTrigger(DodgeRHash);
        animator.ResetTrigger(HitHash);
        animator.ResetTrigger(DeathHash);
        animator.ResetTrigger(CastHash);
        animator.SetBool(BlockHash, false);
    }

#if UNITY_EDITOR
    public int EditorMeleeSwingCount => editorMeleeSwingCount;
    public int EditorMeleeTargetHitCount => editorMeleeTargetHitCount;

    public void EditorResetMeleeQaCounters()
    {
        editorMeleeSwingCount = 0;
        editorMeleeTargetHitCount = 0;
    }

    public int EditorApplyMeleeHitForQa(
        float damage,
        float reach,
        float radius)
    {
        return ApplyMeleeHit(damage, reach, radius);
    }

    public void EditorTriggerLightAttackForQa()
    {
        if (animator &&
            animator.runtimeAnimatorController &&
            animator.isActiveAndEnabled)
            TriggerLightAttack();
    }
#endif

    void OnDrawGizmosSelected()
    {
        Vector3 origin = hitOrigin
            ? hitOrigin.position
            : transform.position + Vector3.up * hitHeight;

        Gizmos.DrawWireSphere(
            origin + transform.forward * lightReach,
            lightRadius);
    }
}
