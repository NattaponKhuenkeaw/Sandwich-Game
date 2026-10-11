using UnityEngine;

namespace SandwichGame
{
    [ExecuteAlways, DefaultExecutionOrder(50), RequireComponent(typeof(Animator))]
    public class GameOverCustomerPose : MonoBehaviour
    {
        public bool HandToForehead;
        void LateUpdate() { ApplyPose(); }

        public void ApplyPose()
        {
            var animator = GetComponent<Animator>();
            if (!animator.isHuman) return;
            var up = transform.up;
            var right = transform.right;
            var forward = transform.forward;
            Aim(animator, HumanBodyBones.LeftUpperArm, HumanBodyBones.LeftLowerArm, -up - right * .15f);
            Aim(animator, HumanBodyBones.LeftLowerArm, HumanBodyBones.LeftHand, -up + forward * .2f);
            Aim(animator, HumanBodyBones.RightUpperArm, HumanBodyBones.RightLowerArm,
                HandToForehead ? right * .8f - up * .3f + forward * .3f : -up + right * .15f);
            Aim(animator, HumanBodyBones.RightLowerArm, HumanBodyBones.RightHand,
                HandToForehead ? up - right * .6f + forward * .3f : -up + forward * .2f);
            var head = animator.GetBoneTransform(HumanBodyBones.Head);
            if (head != null) head.rotation = Quaternion.AngleAxis(22, right) * head.rotation;
        }

        static void Aim(Animator animator, HumanBodyBones parent, HumanBodyBones child, Vector3 direction)
        {
            var bone = animator.GetBoneTransform(parent);
            var end = animator.GetBoneTransform(child);
            if (bone != null && end != null)
                bone.rotation = Quaternion.FromToRotation(end.position - bone.position, direction) * bone.rotation;
        }
    }
}
