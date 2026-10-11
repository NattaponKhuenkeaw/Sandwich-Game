using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
namespace SandwichGame
{
    [ExecuteAlways, RequireComponent(typeof(Animator))]
    public class MenuCustomerPose : MonoBehaviour
    {
        public AnimationClip PoseClip;
        [Min(0f)] public float PoseTime = 0.7f;
        [Range(0f, 0.01f)] public float EyebrowLift = 0.0005f;
        [HideInInspector] public Transform[] EyebrowBones;
        [HideInInspector] public Vector3[] OriginalEyebrowPositions;
        PlayableGraph graph;
        AnimationClipPlayable pose;
        AnimationClip boundClip;
        void OnEnable() { ApplyPose(); }
        void LateUpdate() { ApplyPose(); }
        void OnDisable() { if (graph.IsValid()) graph.Destroy(); boundClip = null; }
        public void ApplyPose()
        {
            if (PoseClip == null) return;
            var animator = GetComponent<Animator>();
            if (!graph.IsValid() || boundClip != PoseClip)
            {
                if (graph.IsValid()) graph.Destroy();
                animator.enabled = true;
                animator.applyRootMotion = false;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                animator.runtimeAnimatorController = null;
                animator.Rebind();
                graph = PlayableGraph.Create("Menu frozen Buff pose");
                graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
                pose = AnimationClipPlayable.Create(graph, PoseClip);
                pose.SetApplyFootIK(false);
                pose.SetSpeed(0);
                var output = AnimationPlayableOutput.Create(graph, "Pose", animator);
                output.SetSourcePlayable(pose);
                graph.Play();
                boundClip = PoseClip;
            }
            pose.SetTime(Mathf.Min(PoseTime, PoseClip.length));
            graph.Evaluate(0.00001f);
            if (EyebrowBones != null && OriginalEyebrowPositions != null)
                for (int i = 0; i < EyebrowBones.Length && i < OriginalEyebrowPositions.Length; i++)
                {
                    var bone = EyebrowBones[i];
                    if (bone != null)
                        bone.localPosition = OriginalEyebrowPositions[i] + bone.parent.InverseTransformVector(transform.up * EyebrowLift);
                }
        }
    }
}
