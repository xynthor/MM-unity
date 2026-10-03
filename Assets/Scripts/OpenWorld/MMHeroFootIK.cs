using UnityEngine;

[DisallowMultipleComponent]
public class MMHeroFootIK : MonoBehaviour
{
    public Animator animator;
    public Transform characterRoot;
    public CharacterController characterController;

    [Header("Grounding")]
    public LayerMask groundLayers = ~0;
    public float rayStartHeight = 0.45f;
    public float rayDistance = 0.95f;
    public float footOffset = 0.035f;

    [Header("Blending")]
    [Range(0f, 1f)] public float maxPositionWeight = 0.9f;
    [Range(0f, 1f)] public float maxRotationWeight = 0.65f;
    public float weightResponse = 14f;

    public bool LastLeftHit { get; private set; }
    public bool LastRightHit { get; private set; }
    public float LeftWeight => leftWeight;
    public float RightWeight => rightWeight;

    float leftWeight;
    float rightWeight;

    void Awake()
    {
        if (!animator)
            animator = GetComponent<Animator>();
        if (!characterRoot && transform.parent)
            characterRoot = transform.parent;
        if (!characterController && characterRoot)
            characterController = characterRoot.GetComponent<CharacterController>();
    }

    void OnAnimatorIK(int layerIndex)
    {
        if (!animator || !animator.isHuman || !animator.isActiveAndEnabled)
            return;

        bool allowIK = !characterController || characterController.isGrounded;

        LastLeftHit = SolveFoot(
            AvatarIKGoal.LeftFoot,
            HumanBodyBones.LeftFoot,
            ref leftWeight,
            allowIK);

        LastRightHit = SolveFoot(
            AvatarIKGoal.RightFoot,
            HumanBodyBones.RightFoot,
            ref rightWeight,
            allowIK);
    }

    bool SolveFoot(
        AvatarIKGoal goal,
        HumanBodyBones bone,
        ref float weight,
        bool allowIK)
    {
        if (!allowIK)
        {
            weight = Mathf.MoveTowards(
                weight,
                0f,
                weightResponse * Time.deltaTime);
            animator.SetIKPositionWeight(goal, weight);
            animator.SetIKRotationWeight(goal, 0f);
            return false;
        }

        Transform foot = animator.GetBoneTransform(bone);
        if (!foot)
        {
            weight = 0f;
            animator.SetIKPositionWeight(goal, 0f);
            animator.SetIKRotationWeight(goal, 0f);
            return false;
        }

        Vector3 origin = foot.position + Vector3.up * rayStartHeight;
        float castDistance = rayStartHeight + rayDistance;

        RaycastHit[] hits = Physics.RaycastAll(
            origin,
            Vector3.down,
            castDistance,
            groundLayers,
            QueryTriggerInteraction.Ignore);

        bool found = false;
        RaycastHit best = default;
        float bestDistance = float.PositiveInfinity;

        Transform ownRoot = characterRoot ? characterRoot : transform.root;

        foreach (RaycastHit hit in hits)
        {
            if (!hit.collider)
                continue;

            Transform hitTransform = hit.collider.transform;
            if (hitTransform == ownRoot || hitTransform.IsChildOf(ownRoot))
                continue;

            if (hit.distance < bestDistance)
            {
                best = hit;
                bestDistance = hit.distance;
                found = true;
            }
        }

        float targetWeight = found ? maxPositionWeight : 0f;
        weight = Mathf.MoveTowards(
            weight,
            targetWeight,
            weightResponse * Time.deltaTime);

        animator.SetIKPositionWeight(goal, weight);
        animator.SetIKRotationWeight(
            goal,
            found ? weight * (maxRotationWeight / Mathf.Max(0.001f, maxPositionWeight)) : 0f);

        if (!found)
            return false;

        Vector3 targetPosition = best.point + best.normal * footOffset;
        animator.SetIKPosition(goal, targetPosition);

        Quaternion currentRotation = animator.GetIKRotation(goal);
        Vector3 projectedForward = Vector3.ProjectOnPlane(
            currentRotation * Vector3.forward,
            best.normal);

        if (projectedForward.sqrMagnitude > 0.0001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(
                projectedForward.normalized,
                best.normal);
            animator.SetIKRotation(goal, targetRotation);
        }

        return true;
    }
}
