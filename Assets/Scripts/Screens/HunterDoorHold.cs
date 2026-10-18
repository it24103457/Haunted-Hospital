using UnityEngine;

/*
 * Pins the cutscene Hunter's hands to two points (the lift door edges) and turns its head towards a point (the camera),
 * using Humanoid IK on top of whatever the Animator is playing. Each weight runs from 0 (ignored) to 1 (fully pinned),
 * so the hold can be faded in when the Hunter grabs the doors and dropped to 0 when it is hit.
 * Lives on the same object as the Hunter's Animator, since Unity only asks components there for IK.
 */
[RequireComponent(typeof(Animator))]
public class HunterDoorHold : MonoBehaviour
{
    public Vector3 leftHand;
    public Vector3 rightHand;
    public Vector3 lookAt;
    [Range(0f, 1f)] public float handWeight;
    [Range(0f, 1f)] public float lookWeight;

    private Animator animator;

    private void Awake()
    {
        animator = GetComponent<Animator>();
    }

    private void OnAnimatorIK(int layerIndex)
    {
        animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, handWeight);
        animator.SetIKPositionWeight(AvatarIKGoal.RightHand, handWeight);
        animator.SetIKPosition(AvatarIKGoal.LeftHand, leftHand);
        animator.SetIKPosition(AvatarIKGoal.RightHand, rightHand);

        animator.SetLookAtWeight(lookWeight, 0.3f, 0.9f, 1f, 0.6f);
        animator.SetLookAtPosition(lookAt);
    }
}
