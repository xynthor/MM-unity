using UnityEngine;

public class MMHeroCombatController : MonoBehaviour
{
    public Animator animator;
    public float comboReset = 0.85f;

    int comboStep;
    float lastAttackTime;

    static readonly int JumpHash = Animator.StringToHash("Jump");
    static readonly int Attack1Hash = Animator.StringToHash("Attack1");
    static readonly int Attack2Hash = Animator.StringToHash("Attack2");
    static readonly int Attack3Hash = Animator.StringToHash("Attack3");
    static readonly int HeavyHash = Animator.StringToHash("HeavyAttack");
    static readonly int KickHash = Animator.StringToHash("Kick");
    static readonly int BlockHash = Animator.StringToHash("Block");
    static readonly int DodgeFHash = Animator.StringToHash("DodgeForward");
    static readonly int DodgeBHash = Animator.StringToHash("DodgeBackward");    static readonly int DodgeLHash = Animator.StringToHash("DodgeLeft");
    static readonly int DodgeRHash = Animator.StringToHash("DodgeRight");
    static readonly int HitHash = Animator.StringToHash("Hit");
    static readonly int DeathHash = Animator.StringToHash("Death");
    static readonly int CastHash = Animator.StringToHash("Cast");

    void Awake()
    {
        if (!animator) animator = GetComponentInChildren<Animator>();
    }

    void Update()
    {
        if (!animator) return;

        if (Time.time - lastAttackTime > comboReset)
            comboStep = 0;

        if (Input.GetMouseButtonDown(0))
        {
            comboStep = (comboStep % 3) + 1;
            lastAttackTime = Time.time;
            animator.SetTrigger(comboStep == 1 ? Attack1Hash : comboStep == 2 ? Attack2Hash : Attack3Hash);
        }

        animator.SetBool(BlockHash, Input.GetMouseButton(1));        if (Input.GetKeyDown(KeyCode.F))
            animator.SetTrigger(HeavyHash);

        if (Input.GetKeyDown(KeyCode.E))
            animator.SetTrigger(KickHash);

        if (Input.GetKeyDown(KeyCode.R))
            animator.SetTrigger(CastHash);

        if (Input.GetButtonDown("Jump"))
            animator.SetTrigger(JumpHash);

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

    public void PlayHit()
    {
        if (animator) animator.SetTrigger(HitHash);
    }    public void PlayDeath()
    {
        if (animator) animator.SetTrigger(DeathHash);
    }

    public void ResetCombat()
    {
        comboStep = 0;
        if (!animator) return;
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
}
